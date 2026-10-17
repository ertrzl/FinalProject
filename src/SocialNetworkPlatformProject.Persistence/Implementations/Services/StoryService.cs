using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.DTOs.Stories;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class StoryService : IStoryService
{
    private const int MaxViewersListed = 200;

    private readonly IStoryRepository _stories;
    private readonly IStoryViewRepository _views;
    private readonly IStoryLikeRepository _likes;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly INotificationService _notifications;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public StoryService(
        IStoryRepository stories,
        IStoryViewRepository views,
        IStoryLikeRepository likes,
        IUserRepository users,
        IFriendService friends,
        INotificationService notifications,
        IFileStorageService files,
        IMapper mapper)
    {
        _stories = stories;
        _views = views;
        _likes = likes;
        _users = users;
        _friends = friends;
        _notifications = notifications;
        _files = files;
        _mapper = mapper;
    }

    public async Task<GetStoryDto> CreateAsync(Guid currentUserId, PostStoryDto dto)
    {
        var isVideo = dto.Media.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        var mediaUrl = isVideo
            ? await _files.SaveVideoAsync(dto.Media, "stories")
            : await _files.SaveImageAsync(dto.Media, "stories");

        var story = new Story
        {
            UserId = currentUserId,
            MediaUrl = mediaUrl,
            MediaType = isVideo ? StoryMediaType.Video : StoryMediaType.Image
        };
        await _stories.AddAsync(story);
        await _stories.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { story }, currentUserId))[0];
    }

    public async Task<List<GetStoryDto>> GetActiveStoriesAsync(Guid currentUserId)
    {
        var visibleUserIds = await _friends.GetFriendIdsAsync(currentUserId);
        visibleUserIds.Add(currentUserId);

        var now = DateTime.UtcNow;
        var stories = await _stories.GetAll(
                filter: s => visibleUserIds.Contains(s.UserId) && s.ExpiresAt > now,
                orderBy: s => s.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        // The story bar plays the stories in this order, so what is new comes first.
        var dtos = await BuildDtosAsync(stories, currentUserId);
        return dtos.OrderBy(s => s.IsViewed).ThenByDescending(s => s.CreatedAt).ToList();
    }

    public async Task DeleteAsync(Guid currentUserId, Guid storyId)
    {
        var story = await _stories.GetByIdAsync(storyId)
            ?? throw new NotFoundException(Loc.T("story.notFound"));

        if (story.UserId != currentUserId)
            throw new ForbiddenException(Loc.T("story.deleteOwn"));

        _stories.Delete(story);
        await _stories.SaveChangesAsync();

        _files.Delete(story.MediaUrl);
    }

    public async Task RecordViewAsync(Guid currentUserId, Guid storyId)
    {
        var story = await _stories.GetByIdAsync(storyId);
        if (story == null || story.ExpiresAt <= DateTime.UtcNow)
            throw new NotFoundException(Loc.T("story.notFound"));

        if (story.UserId == currentUserId)
            return;

        if (!await _friends.AreFriendsAsync(currentUserId, story.UserId))
            throw new NotFoundException(Loc.T("story.notFound"));

        if (await _views.AnyAsync(v => v.StoryId == storyId && v.ViewerId == currentUserId))
            return;

        await _views.AddAsync(new StoryView { StoryId = storyId, ViewerId = currentUserId });
        try
        {
            await _views.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Another tab reported the same view a moment earlier: it is counted already.
        }
    }

    public async Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid storyId)
    {
        var story = await _stories.GetByIdAsync(storyId);
        if (story == null || story.ExpiresAt <= DateTime.UtcNow)
            throw new NotFoundException(Loc.T("story.notFound"));

        if (story.UserId == currentUserId)
            throw new BadRequestException(Loc.T("story.likeOwn"));

        if (!await _friends.AreFriendsAsync(currentUserId, story.UserId))
            throw new NotFoundException(Loc.T("story.notFound"));

        var existing = await _likes.GetAll(l => l.StoryId == storyId && l.UserId == currentUserId).FirstOrDefaultAsync();
        var isLiked = existing == null;

        if (existing != null)
        {
            _likes.Delete(existing);
            await _likes.SaveChangesAsync();
        }
        else
        {
            await _likes.AddAsync(new StoryLike { StoryId = storyId, UserId = currentUserId });
            try
            {
                await _likes.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueViolation())
            {
                // Another tap liked it a moment earlier: it stays liked, and the owner was told by that one.
                return new GetLikeResultDto { IsLiked = true, LikeCount = await CountLikesAsync(storyId) };
            }

            await _notifications.CreateAsync(story.UserId, currentUserId, NotificationType.StoryLiked, storyId: storyId);
        }

        return new GetLikeResultDto { IsLiked = isLiked, LikeCount = await CountLikesAsync(storyId) };
    }

    private async Task<int> CountLikesAsync(Guid storyId)
    {
        return await _likes.GetAll(l => l.StoryId == storyId).CountAsync();
    }

    public async Task<List<GetStoryViewerDto>> GetViewersAsync(Guid currentUserId, Guid storyId)
    {
        var story = await _stories.GetByIdAsync(storyId);
        if (story == null || story.ExpiresAt <= DateTime.UtcNow)
            throw new NotFoundException(Loc.T("story.notFound"));

        if (story.UserId != currentUserId)
            throw new ForbiddenException(Loc.T("story.viewersOwner"));

        var views = await _views.GetNewestAsync(storyId, MaxViewersListed);
        var users = await _users.GetSummariesAsync(views.Select(v => v.ViewerId));
        var likers = await _likes.GetLikerIdsAsync(storyId);

        // A viewer whose account is gone has no summary and is left out.
        return views
            .Where(v => users.ContainsKey(v.ViewerId))
            .Select(v => new GetStoryViewerDto
            {
                UserId = v.ViewerId,
                FullName = users[v.ViewerId].FullName,
                AvatarUrl = users[v.ViewerId].AvatarUrl,
                ViewedAt = v.CreatedAt,
                HasLiked = likers.Contains(v.ViewerId)
            })
            .ToList();
    }

    private async Task<List<GetStoryDto>> BuildDtosAsync(IEnumerable<Story> stories, Guid currentUserId)
    {
        var list = stories.ToList();
        var users = await _users.GetSummariesAsync(list.Select(s => s.UserId));

        // Batched queries for the whole list: view and like counts of my own stories, and which of the others I
        // watched and liked.
        var ownIds = list.Where(s => s.UserId == currentUserId).Select(s => s.Id).ToList();
        var othersIds = list.Where(s => s.UserId != currentUserId).Select(s => s.Id).ToList();
        var viewCounts = await _views.CountByStoryAsync(ownIds);
        var likeCounts = await _likes.CountByStoryAsync(ownIds);
        var viewedIds = await _views.GetViewedStoryIdsAsync(currentUserId, othersIds);
        var likedIds = await _likes.GetLikedStoryIdsAsync(currentUserId, othersIds);

        var dtos = _mapper.Map<List<GetStoryDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (users.TryGetValue(list[i].UserId, out var user))
            {
                dtos[i].UserName = user.FullName;
                dtos[i].UserAvatarUrl = user.AvatarUrl;
            }

            dtos[i].ViewCount = viewCounts.GetValueOrDefault(list[i].Id);
            dtos[i].IsViewed = viewedIds.Contains(list[i].Id);
            dtos[i].LikeCount = likeCounts.GetValueOrDefault(list[i].Id);
            dtos[i].IsLiked = likedIds.Contains(list[i].Id);
        }

        return dtos;
    }
}
