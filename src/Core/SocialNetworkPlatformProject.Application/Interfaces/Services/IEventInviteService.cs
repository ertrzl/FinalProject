using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventInviteService
{
    // The organizer or someone who is going can invite any real user (not only friends). Notifies the invitee.
    Task InviteAsync(Guid currentUserId, Guid eventId, PostEventInviteDto dto);

    // Pending invites on one event (organizer / people going only), so the invite modal can mark who is already invited.
    Task<List<GetEventInviteeDto>> GetInviteesAsync(Guid currentUserId, Guid eventId);

    // Invites addressed to me for events that haven't started, newest first.
    Task<List<GetEventInviteDto>> GetMyInvitesAsync(Guid currentUserId);

    // Same as pressing "Katılıyorum" (so a full event refuses), and the invite is consumed.
    Task<GetEventDto> AcceptAsync(Guid currentUserId, Guid eventId);

    Task DeclineAsync(Guid currentUserId, Guid eventId);
}
