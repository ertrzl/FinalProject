using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        // Notifications are created by services (e.g. when a like/comment/friend-request happens),
        // never posted directly by the client — so there's no PostNotificationDto.
        CreateMap<Notification, GetNotificationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.ActorName, opt => opt.Ignore())
            .ForMember(dest => dest.ActorAvatarUrl, opt => opt.Ignore());
    }
}
