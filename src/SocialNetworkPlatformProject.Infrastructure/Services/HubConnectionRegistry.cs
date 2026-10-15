using Microsoft.AspNetCore.SignalR;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// Every open hub connection with the security stamp of the token it was made with, so the connections of an account that
// was suspended (or deleted, or whose password changed) can be closed at once. Without this a connection kept receiving
// pushes for as long as it stayed open: the per-call check in SecurityStampHubFilter only runs when the client calls something,
// and the notifications hub is listen-only. In-memory: fine for a single server instance.
public class HubConnectionRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Dictionary<string, (string? Stamp, string? SessionId, HubCallerContext Context)>> _connections = new();

    public void Add(Guid userId, string? stamp, string? sessionId, HubCallerContext context)
    {
        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var set))
                _connections[userId] = set = new Dictionary<string, (string?, string?, HubCallerContext)>();

            set[context.ConnectionId] = (stamp, sessionId, context);
        }
    }

    // Closes the connections made in that session (any user: the id is unique); the page then has to renew its token, which
    // fails for an ended session, and sends the person to the sign-in page.
    public int CloseSession(Guid sessionId)
    {
        List<HubCallerContext> doomed;
        lock (_gate)
        {
            doomed = _connections.Values
                .SelectMany(set => set.Values)
                .Where(c => string.Equals(c.SessionId, sessionId.ToString(), StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Context)
                .ToList();
        }

        foreach (var context in doomed)
            context.Abort();

        return doomed.Count;
    }

    public void Remove(Guid userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var set))
                return;

            set.Remove(connectionId);
            if (set.Count == 0)
                _connections.Remove(userId);
        }
    }

    // Closes the connections the user made with that stamp; the client sees them drop and has to sign in again with a token
    // that is still valid (after a password change that is the new one, for a suspended account there is none).
    public int CloseWithStamp(Guid userId, string securityStamp)
    {
        List<HubCallerContext> doomed;
        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var set))
                return 0;

            doomed = set.Values.Where(c => string.Equals(c.Stamp, securityStamp, StringComparison.Ordinal)).Select(c => c.Context).ToList();
        }

        foreach (var context in doomed)
            context.Abort();

        return doomed.Count;
    }
}
