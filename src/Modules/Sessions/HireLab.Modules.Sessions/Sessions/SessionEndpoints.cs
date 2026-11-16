using HireLab.Modules.Sessions.Queue.AddSessionTask;
using HireLab.Modules.Sessions.Queue.GetSessionTask;
using HireLab.Modules.Sessions.Queue.RemoveSessionTask;
using HireLab.Modules.Sessions.Queue.ReorderSessionTasks;
using HireLab.Modules.Sessions.Running.FinishSession;
using HireLab.Modules.Sessions.Running.GoLive;
using HireLab.Modules.Sessions.Running.MarkSessionTaskDone;
using HireLab.Modules.Sessions.Sessions.CreateSession;
using HireLab.Modules.Sessions.Sessions.DeleteSession;
using HireLab.Modules.Sessions.Sessions.GetSession;
using HireLab.Modules.Sessions.Sessions.ListCandidateSessions;
using HireLab.Modules.Sessions.Sessions.ListTeamSessions;
using HireLab.Modules.Sessions.Sessions.RescheduleSession;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions;

internal static class SessionEndpoints
{
    public const string Tag = "Sessions";

    public static void MapSessionEndpoints(this IEndpointRouteBuilder staff)
    {
        var candidateSessions = staff.MapGroup("/candidates/{candidateId:guid}/sessions")
            .WithTags(Tag)
            .RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        candidateSessions.MapListCandidateSessions();
        candidateSessions.MapCreateSession();

        var sessions = staff.MapGroup("/sessions").WithTags(Tag).RequireAuthorization(StaffPolicies.ManagerOrInterviewer);
        sessions.MapListTeamSessions();
        sessions.MapGetSession();
        sessions.MapDeleteSession();
        sessions.MapRescheduleSession();
        sessions.MapGoLive();
        sessions.MapFinishSession();

        var queue = sessions.MapGroup("/{sessionId:guid}/tasks");
        queue.MapAddSessionTask();
        queue.MapRemoveSessionTask();
        queue.MapReorderSessionTasks();
        queue.MapGetSessionTask();
        queue.MapMarkSessionTaskDone();
    }
}
