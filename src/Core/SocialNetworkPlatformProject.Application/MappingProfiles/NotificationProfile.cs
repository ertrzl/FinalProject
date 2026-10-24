using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, GetNotificationDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
            .ForMember(dest => dest.ActorName, opt => opt.Ignore())
            .ForMember(dest => dest.ActorAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.GroupName, opt => opt.Ignore())
            .ForMember(dest => dest.EventTitle, opt => opt.Ignore())
            .ForMember(dest => dest.Reaction, opt => opt.Ignore())
            .ForMember(dest => dest.EventStartsAt, opt => opt.Ignore())
            .ForMember(dest => dest.ListingTitle, opt => opt.Ignore())
            .ForMember(dest => dest.IsPending, opt => opt.Ignore());
    }
}
