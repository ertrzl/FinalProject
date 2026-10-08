using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class PostProfile : Profile
{
    public PostProfile()
    {
        // AuthorName/AuthorAvatarUrl, the like/comment counts and the per-viewer flags (liked, saved, may delete)
        // need a join against ApplicationUser (Persistence-only type), the current user's id or a COUNT query —
        // all are filled in by PostService after this mapping runs, not here.
        CreateMap<Post, GetPostDto>()
            .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy.ToString()))
            .ForMember(dest => dest.MediaType, opt => opt.MapFrom(src => src.MediaType.ToString()))
            .ForMember(dest => dest.IsGroupPost, opt => opt.MapFrom(src => src.GroupId != null))
            .ForMember(dest => dest.LikeCount, opt => opt.Ignore())
            .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
            .ForMember(dest => dest.Hashtags, opt => opt.MapFrom(src => src.Hashtags.Select(h => h.Hashtag!.Name)))
            .ForMember(dest => dest.AuthorName, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.IsSavedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.CanDelete, opt => opt.Ignore())
            .ForMember(dest => dest.CanEdit, opt => opt.Ignore());
    }
}
