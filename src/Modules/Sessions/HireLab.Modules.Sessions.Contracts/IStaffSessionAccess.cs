namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Tells the API host whether the current staff user may follow a session live (ADR 0006): the session exists and the
/// team query filter shows it to them, so another team's session and the admin are refused (ADR 0009). The staff hub
/// asks it before adding a connection to a session's group. Registered as a scoped service by the Sessions module.
/// </summary>
public interface IStaffSessionAccess
{
    /// <summary>Whether the current staff user sees the session <paramref name="sessionId"/>.</summary>
    Task<bool> CanWatchAsync(InterviewSessionId sessionId, CancellationToken cancellationToken = default);
}
