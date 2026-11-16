using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Events;
using Microsoft.AspNetCore.SignalR;

namespace HireLab.Api.Realtime;

/// <summary>
/// The <c>Realtime</c> component's link to the Sessions module (ADR 0006): it turns the module's integration events
/// into thin hub messages for the staff and the candidate watching the session. A connection's cookie is checked only
/// when it opens, so it also drops candidate connections whose cookie stopped working: those of a regenerated link,
/// and all of them once the session has finished.
/// </summary>
internal sealed class SessionEventsRelay(
    IHubContext<StaffHub, IStaffClient> staff,
    IHubContext<CandidateHub, ICandidateClient> candidate,
    CandidateConnections candidateConnections,
    ICandidateAccessValidator candidateAccess)
    : IIntegrationEventHandler<TaskQueueChanged>,
      IIntegrationEventHandler<SessionStateChanged>,
      IIntegrationEventHandler<ResponseUpdated>,
      IIntegrationEventHandler<CandidateLinkRevoked>
{
    public Task HandleAsync(TaskQueueChanged integrationEvent, CancellationToken cancellationToken)
    {
        var group = SessionGroups.Of(integrationEvent.SessionId);
        return Task.WhenAll(
            staff.Clients.Group(group).TaskQueueChanged(integrationEvent.SessionId.Value, integrationEvent.QueueRevision),
            candidate.Clients.Group(group).TaskQueueChanged());
    }

    public async Task HandleAsync(SessionStateChanged integrationEvent, CancellationToken cancellationToken)
    {
        var group = SessionGroups.Of(integrationEvent.SessionId);
        await Task.WhenAll(
            staff.Clients.Group(group).SessionStateChanged(integrationEvent.SessionId.Value),
            candidate.Clients.Group(group).SessionStateChanged());

        foreach (var connection in candidateConnections.Of(integrationEvent.SessionId))
        {
            if ((await candidateAccess.ValidateAsync(connection.Context.User!, cancellationToken)).IsFailure)
            {
                connection.Context.Abort();
            }
        }
    }

    public Task HandleAsync(ResponseUpdated integrationEvent, CancellationToken cancellationToken)
    {
        var group = SessionGroups.Of(integrationEvent.SessionId);
        var sessionTaskId = integrationEvent.SessionTaskId.Value;
        return Task.WhenAll(
            staff.Clients.Group(group).ResponseUpdated(integrationEvent.SessionId.Value, sessionTaskId, integrationEvent.Revision),
            candidate.Clients.Group(group).ResponseUpdated(sessionTaskId, integrationEvent.Revision));
    }

    public Task HandleAsync(CandidateLinkRevoked integrationEvent, CancellationToken cancellationToken)
    {
        foreach (var connection in candidateConnections.Of(integrationEvent.SessionId))
        {
            if (connection.Access.CandidateLinkId == integrationEvent.CandidateLinkId)
            {
                connection.Context.Abort();
            }
        }

        return Task.CompletedTask;
    }
}
