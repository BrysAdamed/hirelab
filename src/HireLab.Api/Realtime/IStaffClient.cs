namespace HireLab.Api.Realtime;

/// <summary>
/// What <c>/hubs/staff</c> pushes to the connections subscribed to a session (ADR 0006). Thin: ids and revisions, and
/// the candidate's presence, which lives only in the hub. The staff app refetches everything else over HTTP.
/// </summary>
public interface IStaffClient
{
    /// <summary>The session's task queue changed; <paramref name="queueRevision"/> is its revision now.</summary>
    Task TaskQueueChanged(Guid sessionId, int queueRevision);

    /// <summary>The session went Live or finished.</summary>
    Task SessionStateChanged(Guid sessionId);

    /// <summary>The candidate saved the response to a session task; <paramref name="revision"/> is its revision now.</summary>
    Task ResponseUpdated(Guid sessionId, Guid sessionTaskId, int revision);

    /// <summary>A candidate connection to the session opened or closed.</summary>
    Task CandidatePresenceChanged(Guid sessionId, CandidatePresence presence);
}
