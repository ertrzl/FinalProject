using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Stories;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class StoryService : IStoryService
{
    private readonly IStoryRepository _stories;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public StoryService(
        IStoryRepository stories,
        IUserRepository users,
        IFriendService friends,
        IFileStorageService files,
        IMapper mapper)
    {
        _stories = stories;
        _users = users;
        _friends = friends;
        _files = files;
        _mapper = mapper;
    }

    public async Task<GetStoryDto> CreateAsync(Guid currentUserId, PostStoryDto dto)
    {
        var imageUrl = await _files.SaveImageAsync(dto.Image, "stories");

        var story = new Story { UserId = currentUserId, ImageUrl = imageUrl };
        await _stories.AddAsync(story);
        await _stories.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { story }))[0];
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

        return await BuildDtosAsync(stories);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid storyId)
    {
        var story = await _stories.GetByIdAsync(storyId)
            ?? throw new NotFoundException("Story not found.");

        if (story.UserId != currentUserId)
            throw new ForbiddenException("You can only delete your own stories.");

        _stories.Delete(story);
        await _stories.SaveChangesAsync();

        _files.Delete(story.ImageUrl);
    }

    private async Task<List<GetStoryDto>> BuildDtosAsync(IEnumerable<Story> stories)
    {
        var list = stories.ToList();
        var users = await _users.GetSummariesAsync(list.Select(s => s.UserId));

        var dtos = _mapper.Map<List<GetStoryDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (users.TryGetValue(list[i].UserId, out var user))
            {
                dtos[i].UserName = user.FullName;
                dtos[i].UserAvatarUrl = user.AvatarUrl;
            }
        }

        return dtos;
    }
}
