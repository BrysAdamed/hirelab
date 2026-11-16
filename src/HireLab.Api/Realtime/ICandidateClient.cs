namespace HireLab.Api.Realtime;

/// <summary>
/// What <c>/hubs/candidate</c> pushes to the candidate's connections (ADR 0006). The session is the cookie's, so no
/// message names it, and none says anything about the queue beyond "it changed". The candidate app refetches its
/// interview over HTTP.
/// </summary>
public interface ICandidateClient
{
    /// <summary>The session's task queue changed, for example the current task was marked done.</summary>
    Task TaskQueueChanged();

    /// <summary>The session went Live or finished.</summary>
    Task SessionStateChanged();

    /// <summary>A response to a task of the session was saved, maybe from another tab; <paramref name="revision"/> is its revision now.</summary>
    Task ResponseUpdated(Guid sessionTaskId, int revision);
}
