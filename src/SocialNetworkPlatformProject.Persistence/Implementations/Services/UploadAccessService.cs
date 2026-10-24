using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class UploadAccessService : IUploadAccessService
{
    private static readonly TimeSpan YesFor = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan NoFor = TimeSpan.FromSeconds(10);

    private readonly IPostRepository _postRows;
    private readonly IStoryRepository _stories;
    private readonly IMessageRepository _messages;
    private readonly IConversationParticipantRepository _participants;
    private readonly IEventRepository _eventRows;
    private readonly IEventPhotoRepository _eventPhotos;
    private readonly IPostAccessService _posts;
    private readonly IFriendService _friends;
    private readonly IEventAccessService _events;
    private readonly IStoryHighlightService _highlights;
    private readonly IMemoryCache _cache;

    public UploadAccessService(
        IPostRepository postRows,
        IStoryRepository stories,
        IMessageRepository messages,
        IConversationParticipantRepository participants,
        IEventRepository eventRows,
        IEventPhotoRepository eventPhotos,
        IPostAccessService posts,
        IFriendService friends,
        IEventAccessService events,
        IStoryHighlightService highlights,
        IMemoryCache cache)
    {
        _postRows = postRows;
        _stories = stories;
        _messages = messages;
        _participants = participants;
        _eventRows = eventRows;
        _eventPhotos = eventPhotos;
        _posts = posts;
        _friends = friends;
        _events = events;
        _highlights = highlights;
        _cache = cache;
    }

    public async Task<bool> CanViewAsync(Guid viewerId, string folder, string url)
    {
        var key = $"upload-access:{viewerId}:{url}";
        if (_cache.TryGetValue(key, out bool known))
            return known;

        var allowed = await DecideAsync(viewerId, folder, url);
        _cache.Set(key, allowed, allowed ? YesFor : NoFor);
        return allowed;
    }

    private async Task<bool> DecideAsync(Guid viewerId, string folder, string url)
    {
        switch (folder)
        {
            case "avatars":
            case "covers":
            case "groups":
            case "group-avatars":
            case "marketplace":
                return true;

            case "posts":
            {
                var post = await _postRows.FindByMediaUrlAsync(url);
                return post != null && await _posts.CanViewAsync(post, viewerId);
            }

            case "stories":
            {
                var story = await _stories.GetAll(s => s.MediaUrl == url, asNoTracking: true).FirstOrDefaultAsync();
                if (story == null || story.ExpiresAt <= DateTime.UtcNow)
                    return false;

                return story.UserId == viewerId || await _friends.AreFriendsAsync(viewerId, story.UserId);
            }

            case "highlights":
                return await _highlights.CanViewFileAsync(viewerId, url);
            case "messages":
            {
                var conversationId = await _messages.GetAll(m => m.MediaUrl == url, asNoTracking: true)
                    .Select(m => (Guid?)m.ConversationId)
                    .FirstOrDefaultAsync();
                if (conversationId == null)
                    return false;

                return await _participants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == viewerId);
            }

            case "chat-avatars":
                return await _participants.AnyAsync(p => p.UserId == viewerId && p.Conversation!.AvatarUrl == url);

            case "events":
            {
                var eventId = await _eventRows.GetAll(e => e.CoverImageUrl == url, asNoTracking: true)
                    .Select(e => (Guid?)e.Id)
                    .FirstOrDefaultAsync();
                return eventId != null && await CanViewEventAsync(viewerId, eventId.Value);
            }

            case "eventphotos":
            {
                var eventId = await _eventPhotos.FindEventIdByUrlAsync(url);
                return eventId != null && await CanViewEventAsync(viewerId, eventId.Value);
            }

            default:
                return false;
        }
    }

    private async Task<bool> CanViewEventAsync(Guid viewerId, Guid eventId)
    {
        try
        {
            await _events.GetViewableAsync(viewerId, eventId);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
    }
}
