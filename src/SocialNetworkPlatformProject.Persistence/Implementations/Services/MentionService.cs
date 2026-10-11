using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MentionService : IMentionService
{
    private readonly IUserRepository _users;
    private readonly IPostAccessService _access;
    private readonly INotificationService _notifications;

    public MentionService(IUserRepository users, IPostAccessService access, INotificationService notifications)
    {
        _users = users;
        _access = access;
        _notifications = notifications;
    }

    public async Task<Dictionary<string, GetMentionDto>> ResolveAsync(IEnumerable<string?> texts)
    {
        var names = texts.SelectMany(MentionParser.ExtractUserNames).Distinct().ToList();
        if (names.Count == 0)
            return new Dictionary<string, GetMentionDto>();

        var users = await _users.GetSummariesByUserNamesAsync(names);
        return users.ToDictionary(
            pair => pair.Key,
            pair => new GetMentionDto { UserId = pair.Value.Id, UserName = pair.Value.UserName, FullName = pair.Value.FullName });
    }

    public async Task NotifyAsync(Post post, Guid actorId, string? text, string? previousText, Guid? commentId = null,
        IReadOnlyCollection<Guid>? alreadyNotified = null)
    {
        var before = MentionParser.ExtractUserNames(previousText).ToHashSet();
        var added = MentionParser.ExtractUserNames(text).Where(name => !before.Contains(name)).ToList();
        if (added.Count == 0)
            return;

        var users = await _users.GetSummariesByUserNamesAsync(added);
        foreach (var user in users.Values.Where(u => u.Id != actorId && alreadyNotified?.Contains(u.Id) != true))
        {
            if (!await _access.CanViewAsync(post, user.Id))
                continue;

            await _notifications.CreateAsync(user.Id, actorId, NotificationType.Mentioned, postId: post.Id, commentId: commentId);
        }
    }
}
