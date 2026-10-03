using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Who may see an event: everyone, except group events, which only the group's members can see.
// Every event service goes through here so the rule lives in one place.
public interface IEventAccessService
{
    // The event, or NotFoundException when it doesn't exist OR the viewer can't see it (a group event they're not in).
    Task<Event> GetViewableAsync(Guid viewerId, Guid eventId, params string[] includes);

    // Groups the viewer belongs to — used to filter event lists.
    Task<List<Guid>> GetMyGroupIdsAsync(Guid viewerId);

    // Keeps only the people who can still see the event (everyone for a public event, current members for a group event).
    Task<List<Guid>> FilterViewersAsync(Event ev, IEnumerable<Guid> userIds);

    Task<bool> IsGroupMemberAsync(Guid groupId, Guid userId);

    // Group events can only be created by the group's admins and moderators.
    Task EnsureCanCreateGroupEventAsync(Guid userId, Guid groupId);
}
