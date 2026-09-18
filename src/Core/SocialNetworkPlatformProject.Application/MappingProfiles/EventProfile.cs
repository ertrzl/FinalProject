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
            .ForMember(dest => dest.GoingCount,
                opt => opt.MapFrom(src => src.Attendees.Count(a => a.Status == EventAttendeeStatus.Going)))
            .ForMember(dest => dest.InterestedCount,
                opt => opt.MapFrom(src => src.Attendees.Count(a => a.Status == EventAttendeeStatus.Interested)))
            .ForMember(dest => dest.CurrentUserStatus, opt => opt.Ignore());
    }
}
