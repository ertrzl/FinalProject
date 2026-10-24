using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PostService : IPostService
{
    private static readonly string[] PostIncludes = { "Hashtags.Hashtag", "Media", "Poll.Options" };

    private const int ExploreWindowDays = 7;

    private const int MaxReactorsListed = 100;

    private static readonly Regex HashtagPattern = new(@"#([\p{L}0-9_]+)", RegexOptions.Compiled);

    private readonly IPostRepository _posts;
    private readonly IPostLikeRepository _likes;
    private readonly ISavedPostRepository _saved;
    private readonly IHashtagRepository _hashtags;
    private readonly IGroupMemberRepository _groupMembers;
    private readonly IGroupService _groupService;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IPostAccessService _access;
    private readonly IMentionService _mentions;
    private readonly IPostMentionService _storedMentions;
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _files;
    private readonly ILiveUpdateService _live;
    private readonly IPostDtoBuilder _dtos;
    private readonly IEventAccessService _eventAccess;

    public PostService(
        IPostRepository posts,
        IPostLikeRepository likes,
        ISavedPostRepository saved,
        IHashtagRepository hashtags,
        IGroupMemberRepository groupMembers,
        IGroupService groupService,
        IUserRepository users,
        IFriendService friends,
        IPostAccessService access,
        IMentionService mentions,
        IPostMentionService storedMentions,
        INotificationService notifications,
        IFileStorageService files,
        ILiveUpdateService live,
        IPostDtoBuilder dtos,
        IEventAccessService eventAccess)
    {
        _posts = posts;
        _likes = likes;
        _saved = saved;
        _hashtags = hashtags;
        _groupMembers = groupMembers;
        _groupService = groupService;
        _users = users;
        _friends = friends;
        _access = access;
        _mentions = mentions;
        _storedMentions = storedMentions;
        _notifications = notifications;
        _files = files;
        _live = live;
        _dtos = dtos;
        _eventAccess = eventAccess;
    }

    public async Task<GetPostDto> CreateAsync(Guid currentUserId, PostPostDto dto)
    {
        var privacy = ParsePrivacy(dto.Privacy);

        if (dto.GroupId.HasValue)
        {
            var isMember = await _groupMembers.AnyAsync(m => m.GroupId == dto.GroupId.Value && m.UserId == currentUserId);
            if (!isMember)
                throw new ForbiddenException(Loc.T("post.groupMember"));
        }

        var post = new Post
        {
            AuthorId = currentUserId,
            Text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim(),
            Privacy = privacy,
            GroupId = dto.GroupId
        };

        foreach (var media in await SaveMediaAsync(dto.Media))
            post.Media.Add(media);

        if (dto.PollOptions.Count > 0)
            post.Poll = NewPoll(dto);

        await _posts.AddAsync(post);
        await SyncHashtagsAsync(post, post.Text);
        await _posts.SaveChangesAsync();

        await _mentions.NotifyAsync(post, currentUserId, post.Text, previousText: null);
        await _storedMentions.SyncAsync(post, post.Text);
        await _live.PostCreatedAsync(post);
        return (await _dtos.BuildAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto)
    {
        var post = await _posts.GetByIdAsync(postId, PostIncludes)
            ?? throw new NotFoundException(Loc.T("post.notFound"));

        if (post.AuthorId != currentUserId)
            throw new ForbiddenException(Loc.T("post.editOwn"));

        var text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim();
        if (text == null && post.Poll != null)
            throw new BadRequestException(Loc.T("post.pollQuestion"));

        if (text == null && post.Media.Count == 0 && !post.IsShare && post.SharedEventId == null)
            throw new BadRequestException(Loc.T("post.needContent"));

        var previousText = post.Text;
        post.Text = text;
        post.Privacy = ParsePrivacy(dto.Privacy);
        await SyncHashtagsAsync(post, text);
        await _posts.SaveChangesAsync();

        await _mentions.NotifyAsync(post, currentUserId, text, previousText);
        await _storedMentions.SyncAsync(post, text);
        await _live.PostUpdatedAsync(post);
        return (await _dtos.BuildAsync(new[] { post }, currentUserId))[0];
    }

    public async Task DeleteAsync(Guid currentUserId, Guid postId, bool asAdmin = false)
    {
        var post = await _posts.GetByIdAsync(postId, "Media")
            ?? throw new NotFoundException(Loc.T("post.notFound"));

        if (!asAdmin && !await _access.CanModerateAsync(post, currentUserId))
            throw new ForbiddenException(Loc.T("post.deleteOwn"));

        await _posts.DetachSharesAsync(new[] { postId });

        var files = post.Media.Select(m => m.Url).ToList();
        _posts.Delete(post);
        await _posts.SaveChangesAsync();

        foreach (var file in files)
            _files.Delete(file);
        await _notifications.DeleteByPostAsync(postId);
        await _live.PostDeletedAsync(post);
    }

    public async Task<GetPostDto> GetByIdAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId, PostIncludes)
            ?? throw new NotFoundException(Loc.T("post.notFound"));

        await _access.EnsureCanViewAsync(post, currentUserId);

        return (await _dtos.BuildAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<PagedResult<GetPostDto>> GetFeedAsync(Guid currentUserId, int page, int pageSize)
    {
        var authorIds = await _friends.GetFriendIdsAsync(currentUserId);
        authorIds.Add(currentUserId);

        return await GetPagedAsync(p => authorIds.Contains(p.AuthorId) && p.GroupId == null, currentUserId, page, pageSize);
    }

    public async Task<PagedResult<GetPostDto>> GetUserPostsAsync(Guid profileUserId, Guid currentUserId, int page, int pageSize)
    {
        var owner = await _users.GetSummaryAsync(profileUserId)
            ?? throw new NotFoundException(Loc.T("user.notFound"));

        var canSeeEverything = profileUserId == currentUserId || await _friends.AreFriendsAsync(profileUserId, currentUserId);

        Expression<Func<Post, bool>> filter;
        if (canSeeEverything)
        {
            filter = p => p.AuthorId == profileUserId && p.GroupId == null;
        }
        else if (owner.IsPrivateAccount)
        {
            return new PagedResult<GetPostDto> { Page = Math.Max(page, 1), PageSize = Math.Clamp(pageSize, 1, 50) };
        }
        else
        {
            filter = p => p.AuthorId == profileUserId && p.GroupId == null && p.Privacy == PostPrivacy.Public;
        }

        return await GetPagedAsync(filter, currentUserId, page, pageSize);
    }

    public async Task<PagedResult<GetPostDto>> GetGroupPostsAsync(Guid groupId, Guid currentUserId, int page, int pageSize)
    {
        await _groupService.GetByIdAsync(currentUserId, groupId);

        return await GetPagedAsync(p => p.GroupId == groupId, currentUserId, page, pageSize);
    }

    public async Task<GetPostReactionResultDto> ToggleLikeAsync(Guid currentUserId, Guid postId)
    {
        var post = await GetViewablePostAsync(currentUserId, postId);
        var existing = await _likes.GetAll(l => l.PostId == postId && l.UserId == currentUserId).FirstOrDefaultAsync();

        return await ApplyReactionAsync(post, currentUserId, existing, existing == null ? ReactionType.Like : null);
    }

    public async Task<GetPostReactionResultDto> SetReactionAsync(Guid currentUserId, Guid postId, string? type)
    {
        var post = await GetViewablePostAsync(currentUserId, postId);
        var existing = await _likes.GetAll(l => l.PostId == postId && l.UserId == currentUserId).FirstOrDefaultAsync();

        ReactionType? wanted = string.IsNullOrEmpty(type) ? null : Enum.Parse<ReactionType>(type);
        return await ApplyReactionAsync(post, currentUserId, existing, wanted);
    }

    public async Task<List<GetPostReactorDto>> GetReactionsAsync(Guid currentUserId, Guid postId, string? type)
    {
        await GetViewablePostAsync(currentUserId, postId);

        ReactionType? filter = null;
        if (!string.IsNullOrEmpty(type))
        {
            if (!Enum.TryParse<ReactionType>(type, out var parsed))
                throw new BadRequestException(Loc.T("post.unknownReaction"));
            filter = parsed;
        }

        var reactions = await _likes.GetNewestAsync(postId, filter, MaxReactorsListed);
        var users = await _users.GetSummariesAsync(reactions.Select(r => r.UserId));

        return reactions
            .Where(r => users.ContainsKey(r.UserId))
            .Select(r => new GetPostReactorDto
            {
                UserId = r.UserId,
                FullName = users[r.UserId].FullName,
                AvatarUrl = users[r.UserId].AvatarUrl,
                Type = r.Type.ToString()
            })
            .ToList();
    }

    private async Task<GetPostReactionResultDto> ApplyReactionAsync(Post post, Guid currentUserId, PostLike? existing, ReactionType? wanted)
    {
        var isNew = existing == null && wanted != null;

        if (existing != null && wanted == null)
            _likes.Delete(existing);
        else if (existing != null && existing.Type != wanted)
            existing.Type = wanted!.Value;
        else if (existing == null && wanted != null)
            await _likes.AddAsync(new PostLike { PostId = post.Id, UserId = currentUserId, Type = wanted.Value });

        await _likes.SaveChangesAsync();

        if (isNew)
        {
            await _notifications.CreateAsync(post.AuthorId, currentUserId, NotificationType.PostLiked,
                postId: post.Id, subject: wanted!.Value.ToString());
        }

        var counts = (await _likes.GetReactionCountsAsync(new[] { post.Id })).GetValueOrDefault(post.Id);
        var reactions = PostDtoBuilder.ToReactionList(counts);
        var total = reactions.Sum(r => r.Count);

        await _live.PostLikeCountChangedAsync(post, currentUserId, total, reactions);
        return new GetPostReactionResultDto
        {
            MyReaction = wanted?.ToString(),
            IsLiked = wanted != null,
            LikeCount = total,
            Reactions = reactions
        };
    }

    public async Task<GetPostDto> ShareAsync(Guid currentUserId, Guid postId, PostShareDto dto)
    {
        var shown = await GetViewablePostAsync(currentUserId, postId);

        if (shown.SharedEventId != null)
            throw new BadRequestException(Loc.T("event.sharedByOther"));

        Post? original = shown;
        if (shown.IsShare)
        {
            original = shown.SharedPostId is { } rootId ? await _posts.GetByIdAsync(rootId) : null;
            if (original == null)
                throw new BadRequestException(Loc.T("post.shareGone"));
        }

        var author = await _users.GetSummaryAsync(original.AuthorId);
        if (original.GroupId != null || original.Privacy != PostPrivacy.Public || author is not { IsPrivateAccount: false })
            throw new BadRequestException(Loc.T("post.shareNot"));

        var share = new Post
        {
            AuthorId = currentUserId,
            Text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim(),
            Privacy = ParsePrivacy(dto.Privacy),
            IsShare = true,
            SharedPostId = original.Id
        };

        await _posts.AddAsync(share);
        await SyncHashtagsAsync(share, share.Text);
        await _posts.SaveChangesAsync();

        await _mentions.NotifyAsync(share, currentUserId, share.Text, previousText: null);
        await _storedMentions.SyncAsync(share, share.Text);
        await _notifications.CreateAsync(original.AuthorId, currentUserId, NotificationType.PostShared, postId: share.Id);
        await _live.PostCreatedAsync(share);

        return (await _dtos.BuildAsync(new[] { share }, currentUserId))[0];
    }

    public async Task<GetPostDto> ShareEventAsync(Guid currentUserId, Guid eventId, PostShareDto dto)
    {
        var ev = await _eventAccess.GetViewableAsync(currentUserId, eventId);

        if (ev.IsPrivate)
            throw new BadRequestException(Loc.T("event.shareNotPrivate"));
        if (ev.GroupId.HasValue)
            throw new BadRequestException(Loc.T("event.shareNotGroup"));

        var share = new Post
        {
            AuthorId = currentUserId,
            Text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim(),
            Privacy = ParsePrivacy(dto.Privacy),
            SharedEventId = ev.Id
        };

        await _posts.AddAsync(share);
        await SyncHashtagsAsync(share, share.Text);
        await _posts.SaveChangesAsync();

        await _mentions.NotifyAsync(share, currentUserId, share.Text, previousText: null);
        await _notifications.CreateAsync(ev.CreatedByUserId, currentUserId, NotificationType.EventShared, postId: share.Id, eventId: ev.Id);
        await _live.PostCreatedAsync(share);

        return (await _dtos.BuildAsync(new[] { share }, currentUserId))[0];
    }

    private async Task<Post> GetViewablePostAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException(Loc.T("post.notFound"));

        await _access.EnsureCanViewAsync(post, currentUserId);
        return post;
    }

    public async Task<bool> ToggleSaveAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException(Loc.T("post.notFound"));

        await _access.EnsureCanViewAsync(post, currentUserId);

        var existing = await _saved.GetAll(s => s.PostId == postId && s.UserId == currentUserId).FirstOrDefaultAsync();

        if (existing != null)
            _saved.Delete(existing);
        else
            await _saved.AddAsync(new SavedPost { PostId = postId, UserId = currentUserId });

        await _saved.SaveChangesAsync();
        return existing == null;
    }

    public async Task<PagedResult<GetPostDto>> GetSavedPostsAsync(Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var visible = _access.VisibleTo(
            _posts.GetAll(p => p.SavedBy.Any(s => s.UserId == currentUserId), asNoTracking: true),
            currentUserId);

        return await PageOfVisibleAsync(
            visible,
            posts => posts.OrderByDescending(p => p.SavedBy.Where(s => s.UserId == currentUserId).Max(s => s.CreatedAt)).ThenBy(p => p.Id),
            currentUserId, page, pageSize);
    }

    private async Task<PagedResult<GetPostDto>> GetPagedAsync(Expression<Func<Post, bool>> filter, Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var visible = _access.VisibleTo(_posts.GetAll(filter, asNoTracking: true), currentUserId);

        return await PageOfVisibleAsync(visible, posts => posts.OrderByDescending(p => p.CreatedAt), currentUserId, page, pageSize);
    }

    private static Poll NewPoll(PostPostDto dto)
    {
        var poll = new Poll { EndsAt = DateTime.UtcNow.AddHours(dto.PollDurationHours ?? PollLimits.DefaultHours) };

        var order = 0;
        foreach (var text in dto.PollOptions)
            poll.Options.Add(new PollOption { Text = text.Trim(), SortOrder = order++ });

        return poll;
    }

    private async Task<List<PostMedia>> SaveMediaAsync(IReadOnlyList<IFormFile> files)
    {
        var saved = new List<PostMedia>();
        try
        {
            foreach (var file in files)
            {
                var isVideo = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
                var url = isVideo
                    ? await _files.SaveVideoAsync(file, "posts")
                    : await _files.SaveImageAsync(file, "posts");

                saved.Add(new PostMedia
                {
                    Url = url,
                    MediaType = isVideo ? PostMediaType.Video : PostMediaType.Image,
                    SortOrder = saved.Count
                });
            }

            return saved;
        }
        catch
        {
            foreach (var media in saved)
                _files.Delete(media.Url);

            throw;
        }
    }

    public async Task<PagedResult<GetPostDto>> SearchAsync(Guid currentUserId, string term, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        term = term.Trim();

        if (term.Length == 0)
            return new PagedResult<GetPostDto> { Page = page, PageSize = pageSize };

        Expression<Func<Post, bool>> filter;
        if (term.StartsWith('#') && term.Length > 1)
        {
            var tag = term[1..].ToLowerInvariant();
            filter = p => p.Hashtags.Any(h => h.Hashtag!.Name == tag);
        }
        else
        {
            var tag = term.ToLowerInvariant();
            filter = p => (p.Text != null && p.Text.Contains(term)) || p.Hashtags.Any(h => h.Hashtag!.Name == tag);
        }

        var visible = _access.VisibleTo(_posts.GetAll(filter, asNoTracking: true), currentUserId);

        return await PageOfVisibleAsync(visible, posts => posts.OrderByDescending(p => p.CreatedAt), currentUserId, page, pageSize);
    }

    public async Task<PagedResult<GetPostDto>> GetPopularPostsAsync(Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var since = DateTime.UtcNow.AddDays(-ExploreWindowDays);
        var visible = _access.VisibleTo(
            _posts.GetAll(p => p.GroupId == null && p.AuthorId != currentUserId && p.CreatedAt >= since, asNoTracking: true),
            currentUserId);

        return await PageOfVisibleAsync(
            visible,
            posts => posts.OrderByDescending(p => p.Likes.Count + 2 * p.Comments.Count).ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            currentUserId, page, pageSize);
    }

    public async Task<List<GetTrendingHashtagDto>> GetTrendingHashtagsAsync(Guid currentUserId, int take)
    {
        take = Math.Clamp(take, 1, 20);

        var since = DateTime.UtcNow.AddDays(-ExploreWindowDays);
        var visible = _access.VisibleTo(_posts.GetAll(p => p.GroupId == null && p.CreatedAt >= since, asNoTracking: true), currentUserId);

        return await visible
            .SelectMany(p => p.Hashtags)
            .GroupBy(link => link.Hashtag!.Name)
            .Select(group => new GetTrendingHashtagDto { Name = group.Key, PostCount = group.Count() })
            .OrderByDescending(tag => tag.PostCount)
            .ThenBy(tag => tag.Name)
            .Take(take)
            .ToListAsync();
    }

    private async Task<PagedResult<GetPostDto>> PageOfVisibleAsync(
        IQueryable<Post> visible, Func<IQueryable<Post>, IOrderedQueryable<Post>> order, Guid currentUserId, int page, int pageSize)
    {
        var total = await visible.CountAsync();
        var pagePosts = await order(visible.Include("Hashtags.Hashtag").Include("Media").Include("Poll.Options"))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GetPostDto>
        {
            Items = await _dtos.BuildAsync(pagePosts, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private static List<string> ExtractHashtagNames(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return HashtagPattern.Matches(text)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Where(name => name.Length <= PostLimits.MaxHashtagLength)
            .Distinct()
            .Take(PostLimits.MaxHashtagsPerPost)
            .ToList();
    }

    private async Task SyncHashtagsAsync(Post post, string? text)
    {
        var names = ExtractHashtagNames(text);

        foreach (var link in post.Hashtags.Where(l => !names.Contains(l.Hashtag!.Name)).ToList())
            post.Hashtags.Remove(link);

        var linked = post.Hashtags.Select(l => l.Hashtag!.Name).ToHashSet();
        var missing = names.Where(name => !linked.Contains(name)).ToList();
        if (missing.Count == 0)
            return;

        var existing = await _hashtags.GetAll(h => missing.Contains(h.Name)).ToListAsync();

        foreach (var name in missing)
        {
            var hashtag = existing.FirstOrDefault(h => h.Name == name);
            if (hashtag == null)
            {
                hashtag = new Hashtag { Name = name };
                await _hashtags.AddAsync(hashtag);
                existing.Add(hashtag);
            }

            await _hashtags.LinkAsync(post, hashtag);
        }
    }

    private static PostPrivacy ParsePrivacy(string value)
    {
        return Enum.TryParse<PostPrivacy>(value, out var privacy)
            ? privacy
            : throw new BadRequestException(Loc.T("post.privacy"));
    }
}
