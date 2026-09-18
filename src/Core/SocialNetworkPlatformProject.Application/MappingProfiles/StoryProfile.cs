using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Stories;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class StoryProfile : Profile
{
    public StoryProfile()
    {
        CreateMap<Story, GetStoryDto>()
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.UserAvatarUrl, opt => opt.Ignore());
    }
}
