using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Friends;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class FriendRequestProfile : Profile
{
    public FriendRequestProfile()
    {
        CreateMap<FriendRequest, GetFriendRequestDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.SenderName, opt => opt.Ignore())
            .ForMember(dest => dest.SenderAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.ReceiverName, opt => opt.Ignore())
            .ForMember(dest => dest.ReceiverAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.MutualFriendsCount, opt => opt.Ignore());
    }
}
