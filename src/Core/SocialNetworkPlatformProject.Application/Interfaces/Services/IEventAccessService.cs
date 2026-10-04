using System.Linq.Expressions;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Who may see an event: everyone, except group events (only that group's members) and private events (only the
// organizer, attendees and invited people).
// Every event service goes through here so the rule lives in one place.
public interface IEventAccessService
{
    // The event, or NotFoundException when it doesn't exist OR the viewer can't see it (a group event they're not in).
    Task<Event> GetViewableAsync(Guid viewerId, Guid eventId, params string[] includes);

    // The same rule as GetViewableAsync, as a filter for event lists (loads the viewer's groups once).
    Task<Expression<Func<Event, bool>>> GetVisibilityFilterAsync(Guid viewerId);

    // Groups the viewer belongs to.
    Task<List<Guid>> GetMyGroupIdsAsync(Guid viewerId);

    // Keeps only the people who can still see the event (everyone for a public event, current members for a group event).
    Task<List<Guid>> FilterViewersAsync(Event ev, IEnumerable<Guid> userIds);

    Task<bool> IsGroupMemberAsync(Guid groupId, Guid userId);

    // Group events can only be created by the group's admins and moderators.
    Task EnsureCanCreateGroupEventAsync(Guid userId, Guid groupId);
}
