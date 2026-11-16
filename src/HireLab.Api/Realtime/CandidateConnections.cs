using HireLab.Modules.Sessions.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Realtime;

/// <summary>A candidate hub connection with the session and link its cookie gave access to.</summary>
internal sealed record CandidateConnection(HubCallerContext Context, CandidateSessionAccess Access);

/// <summary>
/// The open candidate hub connections per session, in memory (ADR 0006): they give the candidate's presence, and the
/// API drops them when the cookie they were opened with stops working, since a connection's cookie is checked only
/// when it opens. A session's last-seen time stays after its last connection closes.
/// </summary>
internal sealed class CandidateConnections(TimeProvider timeProvider)
{
    private readonly Lock _lock = new();
    private readonly Dictionary<InterviewSessionId, SessionConnections> _sessions = [];
    private readonly Dictionary<string, InterviewSessionId> _sessionByConnection = new(StringComparer.Ordinal);

    public CandidatePresence Open(CandidateConnection connection)
    {
        lock (_lock)
        {
            var sessionId = connection.Access.SessionId;
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                session = new SessionConnections();
                _sessions.Add(sessionId, session);
            }

            session.Open[connection.Context.ConnectionId] = connection;
            session.LastSeenAt = timeProvider.GetUtcNow();
            _sessionByConnection[connection.Context.ConnectionId] = sessionId;
            return session.Presence;
        }
    }

    /// <summary>Forgets a closed connection; returns its session and the presence now, or <c>null</c> if it was never opened.</summary>
    public (InterviewSessionId SessionId, CandidatePresence Presence)? Close(string connectionId)
    {
        lock (_lock)
        {
            if (!_sessionByConnection.Remove(connectionId, out var sessionId))
            {
                return null;
            }

            var session = _sessions[sessionId];
            session.Open.Remove(connectionId);
            session.LastSeenAt = timeProvider.GetUtcNow();
            return (sessionId, session.Presence);
        }
    }

    public CandidatePresence PresenceOf(InterviewSessionId sessionId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session.Presence : new CandidatePresence(false, 0, null);
        }
    }

    public IReadOnlyList<CandidateConnection> Of(InterviewSessionId sessionId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? [.. session.Open.Values] : [];
        }
    }

    private sealed class SessionConnections
    {
        public Dictionary<string, CandidateConnection> Open { get; } = new(StringComparer.Ordinal);

        public DateTimeOffset? LastSeenAt { get; set; }

        public CandidatePresence Presence => new(Open.Count > 0, Open.Count, LastSeenAt);
    }
}
