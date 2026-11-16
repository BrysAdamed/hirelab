using HireLab.Modules.Sessions.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Realtime;

/// <summary>
/// <c>/hubs/staff</c> (ADR 0006): a manager or an interviewer subscribes to each session they open and gets its thin
/// events. Subscribing checks that the session is visible to them (the team filter), so another team's session fails
/// like one that doesn't exist. Any number of connections may watch one session.
/// </summary>
internal sealed class StaffHub(IStaffSessionAccess access, CandidateConnections candidates) : Hub<IStaffClient>
{
    public const string Path = "/hubs/staff";

    /// <summary>Starts sending the session's events to this connection and returns the candidate's presence now.</summary>
    public async Task<CandidatePresence> SubscribeToSession(Guid sessionId)
    {
        var id = new InterviewSessionId(sessionId);
        if (!await access.CanWatchAsync(id, Context.ConnectionAborted))
        {
            throw new HubException("The session was not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroups.Of(id), Context.ConnectionAborted);
        return candidates.PresenceOf(id);
    }

    public Task UnsubscribeFromSession(Guid sessionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroups.Of(new InterviewSessionId(sessionId)), Context.ConnectionAborted);
}
