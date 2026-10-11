using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PostDtoBuilder : IPostDtoBuilder
{
    private readonly IPostRepository _posts;
    private readonly IPostLikeRepository _likes;
    private readonly ISavedPostRepository _saved;
    private readonly IPollVoteRepository _votes;
    private readonly IPostAccessService _access;
    private readonly IMentionService _mentions;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public PostDtoBuilder(
        IPostRepository posts,
        IPostLikeRepository likes,
        ISavedPostRepository saved,
        IPollVoteRepository votes,
        IPostAccessService access,
        IMentionService mentions,
        IUserRepository users,
        IMapper mapper)
    {
        _posts = posts;
        _likes = likes;
        _saved = saved;
        _votes = votes;
        _access = access;
        _mentions = mentions;
        _users = users;
        _mapper = mapper;
    }

    public async Task<List<GetPostDto>> BuildAsync(IEnumerable<Post> posts, Guid viewerId)
    {
        var list = posts.ToList();
        var postIds = list.Select(p => p.Id).ToList();

        // The originals of the shares on this page, as far as the viewer may see them.
        var originals = await LoadVisibleOriginalsAsync(list, viewerId);

        var authors = await _users.GetSummariesAsync(list.Select(p => p.AuthorId).Concat(originals.Values.Select(p => p.AuthorId)));
        var mentioned = await _mentions.ResolveAsync(list.Select(p => p.Text).Concat(originals.Values.Select(p => p.Text)));

        var counts = await _posts.GetCountsAsync(postIds);
        var shareCounts = await _posts.GetShareCountsAsync(postIds);
        var reactionCounts = await _likes.GetReactionCountsAsync(postIds);
        var myReactions = await _likes.GetMineAsync(viewerId, postIds);
        var savedIds = (await _saved.GetAll(s => s.UserId == viewerId && postIds.Contains(s.PostId), asNoTracking: true)
            .Select(s => s.PostId)
            .ToListAsync()).ToHashSet();
        var deletableIds = await _access.GetModeratablePostIdsAsync(list, viewerId);

        var polls = list.Where(p => p.Poll != null).Select(p => p.Poll!).ToList();
        var pollIds = polls.Select(p => p.Id).ToList();
        var optionCounts = await _votes.GetCountsByOptionAsync(pollIds);
        var myVotes = await _votes.GetMineAsync(viewerId, pollIds);

        var dtos = _mapper.Map<List<GetPostDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            var post = list[i];
            var dto = dtos[i];

            if (authors.TryGetValue(post.AuthorId, out var author))
            {
                dto.AuthorName = author.FullName;
                dto.AuthorAvatarUrl = author.AvatarUrl;
            }

            dto.LikeCount = counts[post.Id].Likes;
            dto.CommentCount = counts[post.Id].Comments;
            dto.ShareCount = shareCounts.GetValueOrDefault(post.Id);
            dto.Reactions = ToReactionList(reactionCounts.GetValueOrDefault(post.Id));
            dto.MyReaction = myReactions.TryGetValue(post.Id, out var mine) ? mine.ToString() : null;
            dto.IsLikedByCurrentUser = myReactions.ContainsKey(post.Id);
            dto.IsSavedByCurrentUser = savedIds.Contains(post.Id);
            dto.CanDelete = deletableIds.Contains(post.Id);
            dto.CanEdit = post.AuthorId == viewerId;
            dto.Mentions = MentionParser.Pick(post.Text, mentioned);

            if (post.SharedPostId is { } originalId && originals.TryGetValue(originalId, out var original))
                dto.SharedPost = ToSharedDto(original, authors, mentioned);

            if (post.Poll != null)
                dto.Poll = ToPollDto(post, post.Poll, optionCounts, myVotes, viewerId);
        }

        return dtos;
    }

    // The reaction split of one post, biggest first (ties in the order of the reactions).
    public static List<GetReactionCountDto> ToReactionList(IReadOnlyDictionary<ReactionType, int>? counts)
    {
        if (counts == null)
            return new List<GetReactionCountDto>();

        return counts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Select(pair => new GetReactionCountDto { Type = pair.Key.ToString(), Count = pair.Value })
            .ToList();
    }

    private async Task<Dictionary<Guid, Post>> LoadVisibleOriginalsAsync(List<Post> list, Guid viewerId)
    {
        var originalIds = list.Where(p => p.SharedPostId != null).Select(p => p.SharedPostId!.Value).Distinct().ToList();
        if (originalIds.Count == 0)
            return new Dictionary<Guid, Post>();

        var originals = await _posts.GetAll(p => originalIds.Contains(p.Id), asNoTracking: true, includes: new[] { "Media" }).ToListAsync();
        var visible = await _access.FilterVisibleAsync(originals, viewerId);
        return visible.ToDictionary(p => p.Id);
    }

    private GetSharedPostDto ToSharedDto(Post original, Dictionary<Guid, UserSummary> authors, Dictionary<string, GetMentionDto> mentioned)
    {
        var dto = new GetSharedPostDto
        {
            Id = original.Id,
            AuthorId = original.AuthorId,
            Text = original.Text,
            CreatedAt = original.CreatedAt,
            Media = _mapper.Map<List<GetPostMediaDto>>(original.Media.OrderBy(m => m.SortOrder)),
            Mentions = MentionParser.Pick(original.Text, mentioned)
        };

        if (authors.TryGetValue(original.AuthorId, out var author))
        {
            dto.AuthorName = author.FullName;
            dto.AuthorAvatarUrl = author.AvatarUrl;
        }

        return dto;
    }

    // What the viewer may see of a poll: the counts only after voting, when it ended, or for its author.
    private static GetPollDto ToPollDto(
        Post post, Poll poll, Dictionary<Guid, int> optionCounts, Dictionary<Guid, Guid> myVotes, Guid viewerId)
    {
        var ended = poll.IsEnded(DateTime.UtcNow);
        Guid? myOption = myVotes.TryGetValue(poll.Id, out var chosen) ? chosen : null;
        var showResults = myOption != null || ended || post.AuthorId == viewerId;

        var options = poll.Options.OrderBy(o => o.SortOrder).ToList();
        var total = options.Sum(o => optionCounts.GetValueOrDefault(o.Id));

        return new GetPollDto
        {
            Options = options.Select(o =>
            {
                var votes = optionCounts.GetValueOrDefault(o.Id);
                return new GetPollOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    VoteCount = showResults ? votes : 0,
                    Percent = showResults && total > 0 ? (int)Math.Round(100.0 * votes / total) : 0
                };
            }).ToList(),
            TotalVotes = total,
            EndsAt = poll.EndsAt,
            IsEnded = ended,
            MyOptionId = myOption,
            ShowResults = showResults,
            CanVote = !ended && myOption == null
        };
    }
}
