using Microsoft.AspNetCore.SignalR;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

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
