using HireLab.Modules.Sessions.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Realtime;

/// <summary>
/// <c>/hubs/candidate</c> (ADR 0006): every connection joins its own session's group, taken from the candidate cookie,
/// and has no methods to call. Opening and closing a connection changes the candidate's presence, which staff watching
/// the session get at once.
/// </summary>
internal sealed class CandidateHub(
    ICandidateAccessValidator validator,
    CandidateConnections connections,
    IHubContext<StaffHub, IStaffClient> staff) : Hub<ICandidateClient>
{
    public const string Path = "/hubs/candidate";

    public override async Task OnConnectedAsync()
    {
        var access = await validator.ValidateAsync(Context.User!, Context.ConnectionAborted);
        if (access.IsFailure)
        {
            Context.Abort();
            return;
        }

        var sessionId = access.Value.SessionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroups.Of(sessionId), Context.ConnectionAborted);
        var presence = connections.Open(new CandidateConnection(Context, access.Value));
        await staff.Clients.Group(SessionGroups.Of(sessionId)).CandidatePresenceChanged(sessionId.Value, presence);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (connections.Close(Context.ConnectionId) is { } closed)
        {
            await staff.Clients.Group(SessionGroups.Of(closed.SessionId)).CandidatePresenceChanged(closed.SessionId.Value, closed.Presence);
        }
    }
}
