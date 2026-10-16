using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class SessionService : ISessionService
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IAccessTokenRevoker _accessTokens;
    private readonly ILogger<SessionService> _logger;

    public SessionService(IRefreshTokenRepository refreshTokens, IAccessTokenRevoker accessTokens, ILogger<SessionService> logger)
    {
        _refreshTokens = refreshTokens;
        _accessTokens = accessTokens;
        _logger = logger;
    }

    public async Task<List<GetSessionDto>> GetMineAsync(Guid currentUserId, Guid? currentSessionId)
    {
        var active = await _refreshTokens.GetActiveAsync(currentUserId, DateTime.UtcNow);

        // Newest first, so the first row of a session is its latest renewal.
        return active
            .GroupBy(t => t.SessionId)
            .Select(g => g.First())
            .Select(t => new GetSessionDto
            {
                Id = t.SessionId,
                DeviceName = string.IsNullOrWhiteSpace(t.DeviceName) ? Loc.T("session.unknownDevice") : t.DeviceName,
                IpAddress = t.IpAddress,
                StartedAt = t.SessionStartedAt,
                LastActiveAt = t.CreatedAt,
                IsCurrent = currentSessionId == t.SessionId
            })
            .ToList();
    }

    public async Task EndAsync(Guid currentUserId, Guid sessionId)
    {
        if (await _refreshTokens.DeleteSessionAsync(currentUserId, sessionId) == 0)
            throw new NotFoundException(Loc.T("session.notFound"));

        _accessTokens.RevokeSession(sessionId);
        _logger.LogInformation("Session {SessionId:l} of user {UserId:l} was ended from the settings page", sessionId.ToString(), currentUserId.ToString());
    }

    public async Task<int> EndOthersAsync(Guid currentUserId, Guid? currentSessionId)
    {
        var others = (await _refreshTokens.GetActiveAsync(currentUserId, DateTime.UtcNow))
            .Select(t => t.SessionId)
            .Distinct()
            .Where(id => id != currentSessionId)
            .ToList();

        foreach (var sessionId in others)
        {
            await _refreshTokens.DeleteSessionAsync(currentUserId, sessionId);
            _accessTokens.RevokeSession(sessionId);
        }

        if (others.Count > 0)
            _logger.LogInformation("{Count} other session(s) of user {UserId:l} were ended from the settings page", others.Count, currentUserId.ToString());

        return others.Count;
    }
}
