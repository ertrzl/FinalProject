using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class PostProfile : Profile
{
    public PostProfile()
    {
        CreateMap<Post, GetPostDto>()
            .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy.ToString()))
            .ForMember(dest => dest.Media, opt => opt.MapFrom(src => src.Media.OrderBy(m => m.SortOrder)))
            .ForMember(dest => dest.Mentions, opt => opt.Ignore())
            .ForMember(dest => dest.IsGroupPost, opt => opt.MapFrom(src => src.GroupId != null))
            .ForMember(dest => dest.LikeCount, opt => opt.Ignore())
            .ForMember(dest => dest.Reactions, opt => opt.Ignore())
            .ForMember(dest => dest.MyReaction, opt => opt.Ignore())
            .ForMember(dest => dest.ShareCount, opt => opt.Ignore())
            .ForMember(dest => dest.SharedPost, opt => opt.Ignore())
            .ForMember(dest => dest.IsEventShare, opt => opt.MapFrom(src => src.SharedEventId != null))
            .ForMember(dest => dest.SharedEvent, opt => opt.Ignore())
            .ForMember(dest => dest.Poll, opt => opt.Ignore())
            .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
            .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => src.Hashtags.Select(h => h.Hashtag!.Name)))
            .ForMember(dest => dest.AuthorName, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.IsSavedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
            .ForMember(dest => dest.CanEdit, opt => opt.Ignore());

        CreateMap<PostMedia, GetPostMediaDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.MediaType.ToString()));
    }
}
