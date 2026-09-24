using System.Linq.Expressions;
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
    // The mapping profile computes LikeCount/CommentCount from these collections.
    private static readonly string[] CountIncludes = { "Likes", "Comments" };

    private readonly IPostRepository _posts;
    private readonly IPostLikeRepository _likes;
    private readonly ISavedPostRepository _saved;
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

        string? imageUrl = null;
        if (dto.Image != null)
            imageUrl = await _files.SaveImageAsync(dto.Image, "posts");

        var post = new Post
        {
            AuthorId = currentUserId,
            Text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim(),
            ImageUrl = imageUrl,
            Privacy = privacy
        };

        await _posts.AddAsync(post);
        await _posts.SaveChangesAsync();

        await _live.PostCreatedAsync(currentUserId, post.Id);
        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto)
    {
        var post = await _posts.GetByIdAsync(postId, CountIncludes)
            ?? throw new NotFoundException("Post not found.");

        if (post.AuthorId != currentUserId)
            throw new ForbiddenException("You can only edit your own posts.");

        var text = string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim();
        if (text == null && post.ImageUrl == null)
            throw new BadRequestException("A post must have either text or an image.");

        post.Text = text;
        post.Privacy = ParsePrivacy(dto.Privacy);
        await _posts.SaveChangesAsync();

        await _live.PostUpdatedAsync(currentUserId, post.Id);
        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task DeleteAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId)
            ?? throw new NotFoundException("Post not found.");

        if (post.AuthorId != currentUserId)
            throw new ForbiddenException("You can only delete your own posts.");

        _posts.Delete(post);
        await _posts.SaveChangesAsync();

        _files.Delete(post.ImageUrl);
        await _notifications.DeleteByPostAsync(postId);
        await _live.PostDeletedAsync(currentUserId, postId);
    }

    public async Task<GetPostDto> GetByIdAsync(Guid currentUserId, Guid postId)
    {
        var post = await _posts.GetByIdAsync(postId, CountIncludes)
            ?? throw new NotFoundException("Post not found.");

        await _access.EnsureCanViewAsync(post, currentUserId);

        return (await BuildDtosAsync(new[] { post }, currentUserId))[0];
    }

    public async Task<PagedResult<GetPostDto>> GetFeedAsync(Guid currentUserId, int page, int pageSize)
    {
        var authorIds = await _friends.GetFriendIdsAsync(currentUserId);
        authorIds.Add(currentUserId);

        return await GetPagedAsync(p => authorIds.Contains(p.AuthorId), currentUserId, page, pageSize);
    }

    public async Task<PagedResult<GetPostDto>> GetUserPostsAsync(Guid profileUserId, Guid currentUserId, int page, int pageSize)
    {
        var owner = await _users.GetSummaryAsync(profileUserId)
            ?? throw new NotFoundException("User not found.");

        var canSeeEverything = profileUserId == currentUserId || await _friends.AreFriendsAsync(profileUserId, currentUserId);

        Expression<Func<Post, bool>> filter;
        if (canSeeEverything)
        {
            filter = p => p.AuthorId == profileUserId;
        }
        else if (owner.IsPrivateAccount)
        {
            return new PagedResult<GetPostDto> { Page = Math.Max(page, 1), PageSize = Math.Clamp(pageSize, 1, 50) };
        }
        else
        {
            filter = p => p.AuthorId == profileUserId && p.Privacy == PostPrivacy.Public;
        }

        return await GetPagedAsync(filter, currentUserId, page, pageSize);
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
        await _live.PostLikeCountChangedAsync(post.AuthorId, currentUserId, postId, count);
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
        var posts = await _posts.GetAll(p => savedIds.Contains(p.Id), asNoTracking: true, includes: CountIncludes).ToListAsync();
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
                includes: CountIncludes)
            .ToListAsync();

        return new PagedResult<GetPostDto>
        {
            Items = await BuildDtosAsync(posts, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    // Fills in what AutoMapper can't: author display info and the per-viewer liked/saved flags.
    private async Task<List<GetPostDto>> BuildDtosAsync(IEnumerable<Post> posts, Guid currentUserId)
    {
        var list = posts.ToList();
        var postIds = list.Select(p => p.Id).ToList();

        var authors = await _users.GetSummariesAsync(list.Select(p => p.AuthorId));
        var savedIds = (await _saved.GetAll(s => s.UserId == currentUserId && postIds.Contains(s.PostId), asNoTracking: true)
            .Select(s => s.PostId)
            .ToListAsync()).ToHashSet();

        var dtos = _mapper.Map<List<GetPostDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (authors.TryGetValue(list[i].AuthorId, out var author))
            {
                dtos[i].AuthorName = author.FullName;
                dtos[i].AuthorAvatarUrl = author.AvatarUrl;
            }

            dtos[i].IsLikedByCurrentUser = list[i].Likes.Any(l => l.UserId == currentUserId);
            dtos[i].IsSavedByCurrentUser = savedIds.Contains(list[i].Id);
        }

        return dtos;
    }

    private static PostPrivacy ParsePrivacy(string value)
    {
        return Enum.TryParse<PostPrivacy>(value, out var privacy)
            ? privacy
            : throw new BadRequestException("Privacy must be either 'Public' or 'FriendsOnly'.");
    }
}
