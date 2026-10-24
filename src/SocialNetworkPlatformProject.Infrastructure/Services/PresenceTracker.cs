using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class PresenceTracker : IPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, HashSet<string>> _connections = new();

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
