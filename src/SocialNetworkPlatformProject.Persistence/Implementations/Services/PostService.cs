using System.Linq.Expressions;
using System.Text.RegularExpressions;
using AutoMapper;
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
    // The mapping profile builds the hashtag list from this; like/comment counts come from COUNT queries instead.
    private static readonly string[] HashtagIncludes = { "Hashtags.Hashtag" };

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

        string? mediaUrl = null;
        var mediaType = PostMediaType.Image;
        if (dto.Media != null)
        {
            var isVideo = dto.Media.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
            mediaUrl = isVideo
                ? await _files.SaveVideoAsync(dto.Media, "posts")
                : await _files.SaveImageAsync(dto.Media, "posts");
            mediaType = isVideo ? PostMediaType.Video : PostMediaType.Image;
        }

        var post = new Post
        {
            AuthorId = currentUserId,
            Text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim(),
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            Privacy = privacy,
            GroupId = dto.GroupId
        };

        await _posts.AddAsync(post);
        await SyncHashtagsAsync(post, post.Text);
        await _posts.SaveChangesAsync();

        await _live.PostCreatedAsync(post);
        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto)
    {
        var post = await _posts.GetByIdAsync(postId, HashtagIncludes)
            ?? throw new NotFoundException("Post not found.");

        if (post.AuthorId != currentUserId)
            throw new ForbiddenException("You can only edit your own posts.");

        var text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim();
        if (text == null && post.MediaUrl == null)
            throw new BadRequestException("A post must have either text or media.");

        post.Text = text;
        post.Privacy = ParsePrivacy(dto.Privacy);
        await SyncHashtagsAsync(post, text);
        await _posts.SaveChangesAsync();

        await _live.PostUpdatedAsync(post);
        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task DeleteAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException("Post not found.");

        if (!await _access.CanModerateAsync(post, currentUserId))
            throw new ForbiddenException("You can only delete your own posts.");

        _posts.Delete(post);
        await _posts.SaveChangesAsync();

        _files.Delete(post.MediaUrl);
        await _notifications.DeleteByPostAsync(postId);
        await _live.PostDeletedAsync(post);
    }

    public async Task<GetPostDto> GetByIdAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId, HashtagIncludes)
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
        var posts = await _posts.GetAll(p => savedIds.Contains(p.Id), asNoTracking: true, includes: HashtagIncludes).ToListAsync();
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
                includes: HashtagIncludes)
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
        }

        return dtos;
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

        var total = await visible.CountAsync();
        var pagePosts = await visible
            .Include("Hashtags.Hashtag")
            .OrderByDescending(p => p.CreatedAt)
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

    // Replaces post.Hashtags with links matching the post's current text, reusing existing Hashtag rows.
    // Must run before SaveChangesAsync so new Hashtag/PostHashtag rows go in the same write.
    private async Task SyncHashtagsAsync(Post post, string? text)
    {
        post.Hashtags.Clear();

        var names = ExtractHashtagNames(text);
        if (names.Count == 0)
            return;

        var existing = await _hashtags.GetAll(h => names.Contains(h.Name)).ToListAsync();

        foreach (var name in names)
        {
            var hashtag = existing.FirstOrDefault(h => h.Name == name);
            if (hashtag == null)
            {
                hashtag = new Hashtag { Name = name };
                await _hashtags.AddAsync(hashtag);
                existing.Add(hashtag);
            }

            post.Hashtags.Add(new PostHashtag { Post = post, Hashtag = hashtag });
        }
    }

    private static PostPrivacy ParsePrivacy(string value)
    {
        return Enum.TryParse<PostPrivacy>(value, out var privacy)
            ? privacy
            : throw new BadRequestException("Privacy must be either 'Public' or 'FriendsOnly'.");
    }
}
