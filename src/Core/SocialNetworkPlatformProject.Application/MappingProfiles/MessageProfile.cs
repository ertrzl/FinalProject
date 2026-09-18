using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        CreateMap<Message, GetMessageDto>()
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.IsMine, opt => opt.Ignore());

        CreateMap<Conversation, GetConversationDto>()
            .ForMember(dest => dest.OtherUserId, opt => opt.Ignore())
            .ForMember(dest => dest.OtherUserName, opt => opt.Ignore())
            .ForMember(dest => dest.OtherUserAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IsOtherUserOnline, opt => opt.Ignore())
            .ForMember(dest => dest.LastMessageText, opt => opt.Ignore())
            .ForMember(dest => dest.LastMessageAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastMessageIsMine, opt => opt.Ignore())
            .ForMember(dest => dest.UnreadCount, opt => opt.Ignore());
    }
}
