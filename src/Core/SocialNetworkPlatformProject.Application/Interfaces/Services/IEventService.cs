using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The write side of events (reads live in IEventQueryService).
public interface IEventService
{
    Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto);

    // Creator-only, until the event has finished. Notifies the people going/interested when the title, time or place changed.
    Task<GetEventDto> UpdateAsync(Guid currentUserId, Guid eventId, PutEventDto dto);

    Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto);

    Task DeleteAsync(Guid currentUserId, Guid eventId);

    // Called when someone leaves or is removed from a group. They can no longer see its events, so their places on the
    // ones that haven't finished are given up (a freed spot goes to the next person waiting), their pending invitations
    // to them are dropped, and events they organized pass to the group's owner so somebody can still manage them.
    Task RemoveUserFromGroupEventsAsync(Guid userId, Guid groupId, Guid groupOwnerId);
}
