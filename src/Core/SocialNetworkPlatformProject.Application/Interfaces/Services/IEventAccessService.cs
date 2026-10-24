using System.Linq.Expressions;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventAccessService
{
    Task<Event> GetViewableAsync(Guid viewerId, Guid eventId, params string[] includes);

    Task<Expression<Func<Event, bool>>> GetVisibilityFilterAsync(Guid viewerId);

    Task<List<Guid>> GetMyGroupIdsAsync(Guid viewerId);

    Task<List<Guid>> FilterViewersAsync(Event ev, IEnumerable<Guid> userIds);

    Task<bool> IsGroupMemberAsync(Guid groupId, Guid userId);

    Task EnsureCanCreateGroupEventAsync(Guid userId, Guid groupId);
}
