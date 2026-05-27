using System.Collections.Concurrent;

namespace kdspro.Api.Services;

public sealed record PresenceEntry(
    string ConnectionId,
    string UserId,
    string Username,
    string Role,
    string Browser,
    string UserAgent,
    DateTimeOffset LastSeenAt);

public sealed class PresenceTracker
{
    private readonly ConcurrentDictionary<string, PresenceEntry> _connections = new();

    public void Upsert(string connectionId, string userId, string username, string role, string browser, string userAgent)
    {
        var normalizedUserId = string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();
        var normalizedUsername = string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim();
        var normalizedRole = string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToLowerInvariant();
        var normalizedBrowser = string.IsNullOrWhiteSpace(browser) ? "Unknown" : browser.Trim();

        if (string.IsNullOrWhiteSpace(normalizedUserId) || string.IsNullOrWhiteSpace(normalizedRole))
        {
            return;
        }

        _connections[connectionId] = new PresenceEntry(
            ConnectionId: connectionId,
            UserId: normalizedUserId,
            Username: normalizedUsername,
            Role: normalizedRole,
            Browser: normalizedBrowser,
            UserAgent: string.IsNullOrWhiteSpace(userAgent) ? string.Empty : userAgent.Trim(),
            LastSeenAt: DateTimeOffset.UtcNow);
    }

    public void Heartbeat(string connectionId)
    {
        if (!_connections.TryGetValue(connectionId, out var current))
        {
            return;
        }

        _connections[connectionId] = current with { LastSeenAt = DateTimeOffset.UtcNow };
    }

    public void Remove(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public IReadOnlyDictionary<string, PresenceEntry> GetCurrentPresence()
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-60);

        return _connections.Values
            .Where(entry => entry.LastSeenAt >= cutoff)
            .GroupBy(entry => entry.UserId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(entry => entry.LastSeenAt).First(),
                StringComparer.Ordinal);
    }

    public PresenceEntry? GetByConnectionId(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var current) ? current : null;
    }
}
