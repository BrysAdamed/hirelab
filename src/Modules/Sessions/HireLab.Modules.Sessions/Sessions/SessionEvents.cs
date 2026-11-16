using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Events;

namespace HireLab.Modules.Sessions.Sessions;

/// <summary>Raises the Sessions integration events, which the unit of work dispatches after the change is saved.</summary>
internal static class SessionEvents
{
    public static void RaiseQueueChanged(this IIntegrationEventQueue events, InterviewSession session) =>
        events.Enqueue(new TaskQueueChanged(session.Id, session.QueueRevision));

    public static void RaiseStateChanged(this IIntegrationEventQueue events, InterviewSession session) =>
        events.Enqueue(new SessionStateChanged(session.Id));
}
