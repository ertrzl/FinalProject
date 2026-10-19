using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Highlights;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class StoryHighlightService : IStoryHighlightService
{
    // Highlight files live in their own folder, so the clean-up of expired stories never touches them.
    private const string Folder = "highlights";

    private readonly IStoryHighlightRepository _highlights;
    private readonly IStoryHighlightItemRepository _items;
    private readonly IStoryRepository _stories;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly IFileStorageService _files;

    public StoryHighlightService(
        IStoryHighlightRepository highlights,
        IStoryHighlightItemRepository items,
        IStoryRepository stories,
        IUserRepository users,
        IFriendService friends,
        IBlockService blocks,
        IFileStorageService files)
    {
        _highlights = highlights;
        _items = items;
        _stories = stories;
        _users = users;
        _friends = friends;
        _blocks = blocks;
        _files = files;
    }

    public async Task<List<GetHighlightDto>> GetForUserAsync(Guid viewerId, Guid userId, Guid? storyId = null)
    {
        if (!await _users.ExistsAsync(userId))
            throw new NotFoundException(Loc.T("user.notFound"));

        if (!await CanViewAsync(viewerId, userId))
            return new List<GetHighlightDto>();

        var highlights = await _highlights.GetAll(h => h.UserId == userId, orderBy: h => h.CreatedAt, asNoTracking: true).ToListAsync();
        if (highlights.Count == 0)
            return new List<GetHighlightDto>();

        var ids = highlights.Select(h => h.Id).ToList();
        var items = await _items.GetAll(i => ids.Contains(i.HighlightId), orderBy: i => i.StoryCreatedAt, asNoTracking: true)
            .Select(i => new { i.Id, i.HighlightId, i.MediaUrl, i.MediaType, i.SourceStoryId })
            .ToListAsync();
        var byHighlight = items.ToLookup(i => i.HighlightId);

        return highlights.Select(h =>
        {
            var own = byHighlight[h.Id].ToList();
            var cover = own.FirstOrDefault(i => i.Id == h.CoverItemId) ?? own.FirstOrDefault();
            return new GetHighlightDto
            {
                Id = h.Id,
                Title = h.Title,
                ItemCount = own.Count,
                CoverUrl = cover?.MediaUrl,
                CoverMediaType = cover?.MediaType.ToString() ?? "Image",
                // Asked for the owner's own list only: nobody else needs to know which stories are in which highlight.
                ContainsStory = viewerId == userId && storyId != null && own.Any(i => i.SourceStoryId == storyId)
            };
        }).ToList();
    }

    public async Task<GetHighlightDetailDto> GetAsync(Guid viewerId, Guid highlightId)
    {
        var highlight = await _highlights.GetByIdAsync(highlightId, "Items");
        if (highlight == null || !await CanViewAsync(viewerId, highlight.UserId))
            throw new NotFoundException(Loc.T("highlight.notFound"));

        return await BuildDetailAsync(highlight, viewerId);
    }

    public async Task<GetHighlightDetailDto> CreateAsync(Guid currentUserId, PostHighlightDto dto)
    {
        if (await _highlights.GetAll(h => h.UserId == currentUserId).CountAsync() >= HighlightLimits.MaxPerUser)
            throw new BadRequestException(Loc.T("highlight.tooMany", HighlightLimits.MaxPerUser));

        var story = await GetOwnRunningStoryAsync(currentUserId, dto.StoryId);

        var highlight = new StoryHighlight { UserId = currentUserId, Title = dto.Title.Trim() };
        var item = await CopyStoryAsync(story, highlight.Id);
        highlight.Items.Add(item);

        try
        {
            await _highlights.AddAsync(highlight);
            await _highlights.SaveChangesAsync();
        }
        catch
        {
            // The copy was made before the row was; nothing points at it now.
            _files.Delete(item.MediaUrl);
            throw;
        }

        return await BuildDetailAsync(highlight, currentUserId);
    }

    public async Task<GetHighlightDetailDto> AddStoryAsync(Guid currentUserId, Guid highlightId, Guid storyId)
    {
        var highlight = await GetOwnHighlightAsync(currentUserId, highlightId);

        if (highlight.Items.Count >= HighlightLimits.MaxItems)
            throw new BadRequestException(Loc.T("highlight.itemsFull", HighlightLimits.MaxItems));
        if (highlight.Items.Any(i => i.SourceStoryId == storyId))
            throw new ConflictException(Loc.T("highlight.already"));

        var story = await GetOwnRunningStoryAsync(currentUserId, storyId);
        var item = await CopyStoryAsync(story, highlight.Id);

        try
        {
            await _items.AddAsync(item);
            await _items.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Another tap kept the same story a moment earlier: it is in already.
            _files.Delete(item.MediaUrl);
            throw new ConflictException(Loc.T("highlight.already"));
        }
        catch
        {
            _files.Delete(item.MediaUrl);
            throw;
        }

        // Adding the item usually puts it in highlight.Items by itself (EF links it to the highlight it is tracking); make sure for the detail.
        if (!highlight.Items.Contains(item))
            highlight.Items.Add(item);

        return await BuildDetailAsync(highlight, currentUserId);
    }

    public async Task<GetHighlightDetailDto> UpdateAsync(Guid currentUserId, Guid highlightId, PutHighlightDto dto)
    {
        var highlight = await GetOwnHighlightAsync(currentUserId, highlightId);

        if (dto.CoverItemId != null && highlight.Items.All(i => i.Id != dto.CoverItemId))
            throw new BadRequestException(Loc.T("highlight.coverInvalid"));

        highlight.Title = dto.Title.Trim();
        highlight.CoverItemId = dto.CoverItemId;
        await _highlights.SaveChangesAsync();

        return await BuildDetailAsync(highlight, currentUserId);
    }

    public async Task RemoveItemAsync(Guid currentUserId, Guid highlightId, Guid itemId)
    {
        var highlight = await GetOwnHighlightAsync(currentUserId, highlightId);
        var item = highlight.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException(Loc.T("highlight.itemNotFound"));

        // A highlight without a story is nothing to show: taking out the last one deletes it.
        if (highlight.Items.Count == 1)
        {
            await DeleteAsync(currentUserId, highlightId);
            return;
        }

        if (highlight.CoverItemId == item.Id)
            highlight.CoverItemId = null;

        _items.Delete(item);
        await _items.SaveChangesAsync();
        _files.Delete(item.MediaUrl);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid highlightId)
    {
        var highlight = await GetOwnHighlightAsync(currentUserId, highlightId);
        var files = highlight.Items.Select(i => i.MediaUrl).ToList();

        _highlights.Delete(highlight); // the items go with it (cascade)
        await _highlights.SaveChangesAsync();

        foreach (var file in files)
            _files.Delete(file);
    }

    public async Task<bool> CanViewFileAsync(Guid viewerId, string url)
    {
        var ownerId = await _items.FindOwnerByMediaUrlAsync(url);
        return ownerId != null && await CanViewAsync(viewerId, ownerId.Value);
    }

    // Who sees somebody's highlights: the person, their friends, everybody else while the account is open (like the person's public
    // posts); never people who blocked each other.
    private async Task<bool> CanViewAsync(Guid viewerId, Guid ownerId)
    {
        if (viewerId == ownerId)
            return true;

        if (await _blocks.AreBlockedAsync(ownerId, viewerId))
            return false;

        if (await _friends.AreFriendsAsync(ownerId, viewerId))
            return true;

        var owner = await _users.GetSummaryAsync(ownerId);
        return owner is { IsPrivateAccount: false };
    }

    private async Task<StoryHighlight> GetOwnHighlightAsync(Guid currentUserId, Guid highlightId)
    {
        var highlight = await _highlights.GetByIdAsync(highlightId, "Items")
            ?? throw new NotFoundException(Loc.T("highlight.notFound"));

        if (highlight.UserId != currentUserId)
            throw new ForbiddenException(Loc.T("highlight.ownOnly"));

        return highlight;
    }

    // Somebody else's story, or one that is over, is "not found" (the same answer for both, so ids cannot be probed).
    private async Task<Story> GetOwnRunningStoryAsync(Guid currentUserId, Guid storyId)
    {
        var story = await _stories.GetByIdAsync(storyId);
        if (story == null || story.UserId != currentUserId || story.ExpiresAt <= DateTime.UtcNow)
            throw new NotFoundException(Loc.T("story.notFound"));

        return story;
    }

    // The item for a story: its file copied (the story's own file is deleted when the story expires) and its song noted.
    private async Task<StoryHighlightItem> CopyStoryAsync(Story story, Guid highlightId)
    {
        return new StoryHighlightItem
        {
            HighlightId = highlightId,
            MediaUrl = await _files.CopyAsync(story.MediaUrl, Folder),
            MediaType = story.MediaType,
            MusicTitle = story.MusicTitle,
            MusicArtist = story.MusicArtist,
            MusicUrl = story.MusicUrl,
            MusicArtworkUrl = story.MusicArtworkUrl,
            SourceStoryId = story.Id,
            StoryCreatedAt = story.CreatedAt
        };
    }

    private async Task<GetHighlightDetailDto> BuildDetailAsync(StoryHighlight highlight, Guid viewerId)
    {
        var owner = await _users.GetSummaryAsync(highlight.UserId);
        var items = highlight.Items.OrderBy(i => i.StoryCreatedAt).ThenBy(i => i.CreatedAt).ToList();
        var cover = items.FirstOrDefault(i => i.Id == highlight.CoverItemId) ?? items.FirstOrDefault();

        return new GetHighlightDetailDto
        {
            Id = highlight.Id,
            Title = highlight.Title,
            OwnerId = highlight.UserId,
            OwnerName = owner?.FullName ?? string.Empty,
            OwnerAvatarUrl = owner?.AvatarUrl,
            IsOwn = highlight.UserId == viewerId,
            CoverItemId = highlight.CoverItemId,
            EffectiveCoverItemId = cover?.Id,
            Items = items.Select(i => new GetHighlightItemDto
            {
                Id = i.Id,
                MediaUrl = i.MediaUrl,
                MediaType = i.MediaType.ToString(),
                MusicTitle = i.MusicTitle,
                MusicArtist = i.MusicArtist,
                MusicUrl = i.MusicUrl,
                MusicArtworkUrl = i.MusicArtworkUrl,
                CreatedAt = i.StoryCreatedAt
            }).ToList()
        };
    }
}
