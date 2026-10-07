using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class EventProfile : Profile
{
    public EventProfile()
    {
        CreateMap<Event, GetEventDto>()
            // The head-counts come from EventDtoBuilder (an in-memory count or a COUNT query), not from here:
            // a list of events must not have to load every attendee just to be counted.
            .ForMember(dest => dest.GoingCount, opt => opt.Ignore())
            .ForMember(dest => dest.InterestedCount, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentUserStatus, opt => opt.Ignore())
            .ForMember(dest => dest.IsOwner, opt => opt.Ignore())
            .ForMember(dest => dest.GroupName, opt => opt.Ignore())
            .ForMember(dest => dest.InvitedByName, opt => opt.Ignore())
            .ForMember(dest => dest.IsFull, opt => opt.Ignore())
            .ForMember(dest => dest.SpotsLeft, opt => opt.Ignore())
            .ForMember(dest => dest.WaitlistCount, opt => opt.Ignore())
            .ForMember(dest => dest.MyWaitlistPosition, opt => opt.Ignore())
            .ForMember(dest => dest.OnlineLink, opt => opt.Ignore()) // revealed per viewer in EventService.ToDto
            .ForMember(dest => dest.CreatedByName, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IsOngoing, opt => opt.MapFrom(src => src.StartsAt <= DateTime.UtcNow && src.EndsAt > DateTime.UtcNow))
            .ForMember(dest => dest.IsPast, opt => opt.MapFrom(src => src.EndsAt <= DateTime.UtcNow));
    }
}
