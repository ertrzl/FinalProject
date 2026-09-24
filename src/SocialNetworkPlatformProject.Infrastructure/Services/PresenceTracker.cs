using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// Counts live SignalR connections per user (one per open tab). In-memory: fine for a single server instance.
public class PresenceTracker : IPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, HashSet<string>> _connections = new();

    // Returns true when this was the user's first connection (they just came online).
    public bool Connected(Guid userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var set))
                _connections[userId] = set = new HashSet<string>();

            set.Add(connectionId);
            return set.Count == 1;
        }
    }

    // Returns true when that was the user's last connection.
    public bool Disconnected(Guid userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connections.TryGetValue(userId, out var set))
                return false;

            set.Remove(connectionId);
            if (set.Count > 0)
                return false;

            _connections.Remove(userId);
            return true;
        }
    }

    public bool IsOnline(Guid userId)
    {
        lock (_gate)
        {
            return _connections.ContainsKey(userId);
        }
    }
}
