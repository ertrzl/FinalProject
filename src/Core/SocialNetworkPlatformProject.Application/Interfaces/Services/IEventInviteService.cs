using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventInviteService
{
    Task InviteAsync(Guid currentUserId, Guid eventId, PostEventInviteDto dto);

    Task<List<GetEventInviteeDto>> GetInviteesAsync(Guid currentUserId, Guid eventId);

    Task<List<GetEventInviteDto>> GetMyInvitesAsync(Guid currentUserId);

    Task<GetEventDto> AcceptAsync(Guid currentUserId, Guid eventId);

    Task DeclineAsync(Guid currentUserId, Guid eventId);
}
