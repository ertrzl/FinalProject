using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMentionService
{
    Task<Dictionary<string, GetMentionDto>> ResolveAsync(IEnumerable<string?> texts);

    Task NotifyAsync(Post post, Guid actorId, string? text, string? previousText, Guid? commentId = null,
        IReadOnlyCollection<Guid>? alreadyNotified = null);
}
