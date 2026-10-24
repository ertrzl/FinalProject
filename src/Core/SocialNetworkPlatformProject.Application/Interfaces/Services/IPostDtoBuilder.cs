using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPostDtoBuilder
{
    Task<List<GetPostDto>> BuildAsync(IEnumerable<Post> posts, Guid viewerId);
}
