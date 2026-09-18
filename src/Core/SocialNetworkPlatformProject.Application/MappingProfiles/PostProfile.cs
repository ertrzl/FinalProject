using AutoMapper;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.MappingProfiles;

public class PostProfile : Profile
{
    public PostProfile()
    {
        // AuthorName/AuthorAvatarUrl and the IsLikedByCurrentUser/IsSavedByCurrentUser flags
        // need a join against ApplicationUser (Persistence-only type) and the current user's id —
        // both are filled in by PostService after this mapping runs, not here.
        CreateMap<Post, GetPostDto>()
            .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy.ToString()))
            .ForMember(dest => dest.LikeCount, opt => opt.MapFrom(src => src.Likes.Count))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count))
            .ForMember(dest => dest.AuthorName, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.Ignore())
            .ForMember(dest => dest.IsLikedByCurrentUser, opt => opt.Ignore())
            .ForMember(dest => dest.IsSavedByCurrentUser, opt => opt.Ignore());
    }
}
