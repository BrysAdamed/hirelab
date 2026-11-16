using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Queue.GetSessionTask;

internal static class GetSessionTaskEndpoint
{
    public static RouteHandlerBuilder MapGetSessionTask(this IEndpointRouteBuilder queue) =>
        queue.MapGet("/{sessionTaskId:guid}", async (
                InterviewSessionId sessionId,
                SessionTaskId sessionTaskId,
                IQueryHandler<GetSessionTaskQuery, Result<SessionTaskDetailResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetSessionTaskQuery(sessionId, sessionTaskId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("GetSessionTask")
            .WithSummary(
                "Gets a task of a session's queue with the content, reference answer and interviewer notes of the version it "
                + "pins, and the candidate's response with its revision.");
}
