using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Groups;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class GroupProfile : Profile
{
    public GroupProfile()
    {
        CreateMap<Group, GetGroupDto>()
            .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy.ToString()))
            .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count))
            .ForMember(dest => dest.IsCurrentUserMember, opt => opt.Ignore())
            .ForMember(dest => dest.IsCurrentUserAdmin, opt => opt.Ignore())
            .ForMember(dest => dest.IsCurrentUserModerator, opt => opt.Ignore())
            .ForMember(dest => dest.IsCurrentUserOwner, opt => opt.Ignore())
            .ForMember(dest => dest.Members, opt => opt.Ignore());
    }
}
