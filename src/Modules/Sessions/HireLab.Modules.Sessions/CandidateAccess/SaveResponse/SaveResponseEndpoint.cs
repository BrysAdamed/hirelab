using System.Security.Claims;
using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.CandidateAccess.SaveResponse;

/// <summary>
/// The candidate's whole response to the session task, and the revision it was based on (0 before the first save). The
/// task is in the body because candidate routes carry no ids (ADR 0008).
/// </summary>
internal sealed record SaveResponseRequest(Guid SessionTaskId, JsonElement Response, int Revision)
{
    public override string ToString() => nameof(SaveResponseRequest);
}

internal static class SaveResponseEndpoint
{
    public const long MaxRequestBodySize = Response.MaxBytes + (64 * 1024);

    public static RouteHandlerBuilder MapSaveResponse(this IEndpointRouteBuilder candidate) =>
        candidate.MapPut("/session/response", HandleAsync)
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBodySize))
            .WithName("SaveResponse")
            .WithSummary(
                "Saves the candidate's whole response to the current task, named in the body, based on the revision they last "
                + "saw, and returns the new revision. Refused for an older revision (409 Response.StaleRevision), a task that "
                + "isn't Current (409 InterviewSession.TaskNotCurrent), a session that isn't Live (409 InterviewSession.NotLive), "
                + "and a response the task type rejects or that is larger than 1 MB (400).");

    private static async Task<Results<Ok<SavedResponse>, ProblemHttpResult>> HandleAsync(
        SaveResponseRequest request,
        ClaimsPrincipal user,
        ICommandHandler<SaveResponseCommand, Result<SavedResponse>> handler,
        CancellationToken cancellationToken)
    {
        if (CandidateCookie.From(user) is not { } cookie)
        {
            return CandidateAccessErrors.InvalidLink.ToProblem();
        }

        var command = new SaveResponseCommand(cookie, new SessionTaskId(request.SessionTaskId), request.Response, request.Revision);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
