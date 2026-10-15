using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class DataCleanupService : IDataCleanupService
{
    private const int DefaultNotificationDays = 90;
    private const int DefaultAuditLogDays = 365;

    // A hashtag row is created together with the post that uses it; leaving a day of room means a tag that a post is being
    // saved with right now can never be removed under it.
    private static readonly TimeSpan HashtagGracePeriod = TimeSpan.FromDays(1);

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly INotificationRepository _notifications;
    private readonly IHashtagRepository _hashtags;
    private readonly IAuditLogRepository _auditLogs;
    private readonly ILogger<DataCleanupService> _logger;
    private readonly TimeSpan _notificationAge;
    private readonly TimeSpan _auditLogAge;

    public DataCleanupService(
        IRefreshTokenRepository refreshTokens,
        INotificationRepository notifications,
        IHashtagRepository hashtags,
        IAuditLogRepository auditLogs,
        IConfiguration configuration,
        ILogger<DataCleanupService> logger)
    {
        _refreshTokens = refreshTokens;
        _notifications = notifications;
        _hashtags = hashtags;
        _auditLogs = auditLogs;
        _logger = logger;
        _notificationAge = TimeSpan.FromDays(Math.Max(configuration.GetValue("Maintenance:NotificationDays", DefaultNotificationDays), 1));
        _auditLogAge = TimeSpan.FromDays(Math.Max(configuration.GetValue("Maintenance:AuditLogDays", DefaultAuditLogDays), 1));
    }

    public async Task<int> CleanUpAsync(DateTime nowUtc)
    {
        // A token that has expired can't be used and can't prove anything any more, signed out or not.
        var tokens = await _refreshTokens.DeleteAllExpiredAsync(nowUtc);
        var notifications = await _notifications.DeleteOlderThanAsync(nowUtc - _notificationAge);
        var hashtags = await _hashtags.DeleteUnusedCreatedBeforeAsync(nowUtc - HashtagGracePeriod);
        var auditLines = await _auditLogs.DeleteOlderThanAsync(nowUtc - _auditLogAge);

        if (tokens + notifications + hashtags + auditLines > 0)
        {
            _logger.LogInformation("Clean-up: {Tokens} expired sign-in token(s), {Notifications} old notification(s), {Hashtags} unused hashtag(s), {AuditLines} old admin log line(s) removed.",
                tokens, notifications, hashtags, auditLines);
        }

        return tokens + notifications + hashtags + auditLines;
    }
}
