using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PostService : IPostService
{
    // The mapping profile builds the hashtag and media lists from these; like/comment counts come from COUNT queries instead.
    private static readonly string[] PostIncludes = { "Hashtags.Hashtag", "Media" };

    // Keşfet looks at the last week: what is popular is what is being talked about now.
    private const int ExploreWindowDays = 7;

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
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _files;
    private readonly ILiveUpdateService _live;
    private readonly IMapper _mapper;

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
        INotificationService notifications,
        IFileStorageService files,
        ILiveUpdateService live,
        IMapper mapper)
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
        _notifications = notifications;
        _files = files;
        _live = live;
        _mapper = mapper;
    }

    public async Task<GetPostDto> CreateAsync(Guid currentUserId, PostPostDto dto)
    {
        var privacy = ParsePrivacy(dto.Privacy);

        if (dto.GroupId.HasValue)
        {
            var isMember = await _groupMembers.AnyAsync(m => m.GroupId == dto.GroupId.Value && m.UserId == currentUserId);
            if (!isMember)
                throw new ForbiddenException("You must be a member of this group to post in it.");
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

        await _posts.AddAsync(post);
        await SyncHashtagsAsync(post, post.Text);
        await _posts.SaveChangesAsync();

        await _mentions.NotifyAsync(post, currentUserId, post.Text, previousText: null);
        await _live.PostCreatedAsync(post);
        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto)
    {
        var post = await _posts.GetByIdAsync(postId, PostIncludes)
            ?? throw new NotFoundException("Post not found.");

        if (post.AuthorId != currentUserId)
            throw new ForbiddenException("You can only edit your own posts.");

        var text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim();
        if (text == null && post.Media.Count == 0)
            throw new BadRequestException("A post must have either text or media.");

        var previousText = post.Text;
        post.Text = text;
        post.Privacy = ParsePrivacy(dto.Privacy);
        await SyncHashtagsAsync(post, text);
        await _posts.SaveChangesAsync();

        await _mentions.NotifyAsync(post, currentUserId, text, previousText);
        await _live.PostUpdatedAsync(post);
        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task DeleteAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId, "Media")
            ?? throw new NotFoundException("Post not found.");

        if (!await _access.CanModerateAsync(post, currentUserId))
            throw new ForbiddenException("You can only delete your own posts.");

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
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<PagedResult<GetPostDto>> GetFeedAsync(Guid currentUserId, int page, int pageSize)
    {
        var authorIds = await _friends.GetFriendIdsAsync(currentUserId);
        authorIds.Add(currentUserId);

        // Group posts only appear on their group's own wall, not in the personal feed.
        return await GetPagedAsync(p => authorIds.Contains(p.AuthorId) && p.GroupId == null, currentUserId, page, pageSize);
    }

    public async Task<PagedResult<GetPostDto>> GetUserPostsAsync(Guid profileUserId, Guid currentUserId, int page, int pageSize)
    {
        var owner = await _users.GetSummaryAsync(profileUserId)
            ?? throw new NotFoundException("User not found.");

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
        // Throws NotFoundException if the group doesn't exist or is private and the viewer isn't a member.
        await _groupService.GetByIdAsync(currentUserId, groupId);

        return await GetPagedAsync(p => p.GroupId == groupId, currentUserId, page, pageSize);
    }

    public async Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        var existing = await _likes.GetAll(l => l.PostId == postId && l.UserId == currentUserId).FirstOrDefaultAsync();
        var isLiked = existing == null;

        if (existing != null)
            _likes.Delete(existing);
        else
            await _likes.AddAsync(new PostLike { PostId = postId, UserId = currentUserId });

        await _likes.SaveChangesAsync();

        if (isLiked)
            await _notifications.CreateAsync(post.AuthorId, currentUserId, NotificationType.PostLiked, postId: postId);

        var count = await _likes.GetAll(l => l.PostId == postId).CountAsync();
        await _live.PostLikeCountChangedAsync(post, currentUserId, count);
        return new GetLikeResultDto { IsLiked = isLiked, LikeCount = count };
    }

    public async Task<bool> ToggleSaveAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException("Post not found.");

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

        // A user's saved list stays small, so it's filtered for visibility and paged in memory:
        // a post may have turned private since it was saved and must silently drop out.
        var savedEntries = await _saved.GetAll(
                filter: s => s.UserId == currentUserId,
                orderBy: s => s.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        var savedIds = savedEntries.Select(s => s.PostId).ToList();
        var posts = await _posts.GetAll(p => savedIds.Contains(p.Id), asNoTracking: true, includes: PostIncludes).ToListAsync();
        var visible = await _access.FilterVisibleAsync(posts, currentUserId);

        var savedOrder = savedIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var pagePosts = visible
            .OrderBy(p => savedOrder[p.Id])
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<GetPostDto>
        {
            Items = await BuildDtosAsync(pagePosts, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = visible.Count
        };
    }

    private async Task<PagedResult<GetPostDto>> GetPagedAsync(Expression<Func<Post, bool>> filter, Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var total = await _posts.GetAll(filter).CountAsync();

        var posts = await _posts.GetAll(
                filter: filter,
                orderBy: p => p.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                page: page,
                take: pageSize,
                includes: PostIncludes)
            .ToListAsync();

        return new PagedResult<GetPostDto>
        {
            Items = await BuildDtosAsync(posts, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    // Fills in what AutoMapper can't: author display info, the counts and the per-viewer liked/saved/may-delete flags.
    private async Task<List<GetPostDto>> BuildDtosAsync(IEnumerable<Post> posts, Guid currentUserId)
    {
        var list = posts.ToList();
        var postIds = list.Select(p => p.Id).ToList();

        var authors = await _users.GetSummariesAsync(list.Select(p => p.AuthorId));
        var counts = await _posts.GetCountsAsync(postIds);
        var likedIds = (await _likes.GetAll(l => l.UserId == currentUserId && postIds.Contains(l.PostId), asNoTracking: true)
            .Select(l => l.PostId)
            .ToListAsync()).ToHashSet();
        var savedIds = (await _saved.GetAll(s => s.UserId == currentUserId && postIds.Contains(s.PostId), asNoTracking: true)
            .Select(s => s.PostId)
            .ToListAsync()).ToHashSet();
        var deletableIds = await _access.GetModeratablePostIdsAsync(list, currentUserId);
        var mentioned = await _mentions.ResolveAsync(list.Select(p => p.Text));

        var dtos = _mapper.Map<List<GetPostDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (authors.TryGetValue(list[i].AuthorId, out var author))
            {
                dtos[i].AuthorName = author.FullName;
                dtos[i].AuthorAvatarUrl = author.AvatarUrl;
            }

            dtos[i].LikeCount = counts[list[i].Id].Likes;
            dtos[i].CommentCount = counts[list[i].Id].Comments;
            dtos[i].IsLikedByCurrentUser = likedIds.Contains(list[i].Id);
            dtos[i].IsSavedByCurrentUser = savedIds.Contains(list[i].Id);
            dtos[i].CanDelete = deletableIds.Contains(list[i].Id);
            dtos[i].CanEdit = list[i].AuthorId == currentUserId;
            dtos[i].Mentions = MentionParser.Pick(list[i].Text, mentioned);
        }

        return dtos;
    }

    // Stores the files of a new post in the order given. A file that is refused (not an image, too big...) fails the
    // whole post, and the files stored before it are removed again instead of being left behind.
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

        // Visibility is part of the query, so the database counts and pages only what the viewer may see.
        var visible = _access.VisibleTo(_posts.GetAll(filter, asNoTracking: true), currentUserId);

        return await PageOfVisibleAsync(visible, posts => posts.OrderByDescending(p => p.CreatedAt), currentUserId, page, pageSize);
    }

    public async Task<PagedResult<GetPostDto>> GetPopularPostsAsync(Guid currentUserId, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        // Other people's recent posts the viewer may see (never group walls); what the viewer wrote is not "discovery".
        var since = DateTime.UtcNow.AddDays(-ExploreWindowDays);
        var visible = _access.VisibleTo(
            _posts.GetAll(p => p.GroupId == null && p.AuthorId != currentUserId && p.CreatedAt >= since, asNoTracking: true),
            currentUserId);

        // Most talked about first: a comment counts double a like. Newest first among equals.
        return await PageOfVisibleAsync(
            visible,
            posts => posts.OrderByDescending(p => p.Likes.Count + 2 * p.Comments.Count).ThenByDescending(p => p.CreatedAt).ThenBy(p => p.Id),
            currentUserId, page, pageSize);
    }

    public async Task<List<GetTrendingHashtagDto>> GetTrendingHashtagsAsync(Guid currentUserId, int take)
    {
        take = Math.Clamp(take, 1, 20);

        // Counted over the posts the viewer may see, so a tag's number matches what clicking it will show.
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

    // One page of an already visibility-filtered query: the database counts and pages, only the page is loaded.
    private async Task<PagedResult<GetPostDto>> PageOfVisibleAsync(
        IQueryable<Post> visible, Func<IQueryable<Post>, IOrderedQueryable<Post>> order, Guid currentUserId, int page, int pageSize)
    {
        var total = await visible.CountAsync();
        var pagePosts = await order(visible.Include("Hashtags.Hashtag").Include("Media"))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GetPostDto>
        {
            Items = await BuildDtosAsync(pagePosts, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    // Post text -> distinct, lowercased hashtag names ("Merhaba #Kod #kod!" -> ["kod"]).
    private static List<string> ExtractHashtagNames(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return HashtagPattern.Matches(text)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    // Makes the post's hashtag links match its current text: links whose tag is no longer in the text are removed,
    // missing ones are added (reusing existing Hashtag rows), the rest are left alone. For an existing post its
    // Hashtags (with their Hashtag) must be loaded. Must run before SaveChangesAsync so everything goes in one write.
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
            : throw new BadRequestException("Privacy must be either 'Public' or 'FriendsOnly'.");
    }
}
