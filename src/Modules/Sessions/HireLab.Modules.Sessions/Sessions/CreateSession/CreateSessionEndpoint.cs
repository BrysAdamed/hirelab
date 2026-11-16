using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.Sessions.CreateSession;

internal sealed record CreateSessionRequest(DateTimeOffset? ScheduledAt);

internal static class CreateSessionEndpoint
{
    public static RouteHandlerBuilder MapCreateSession(this IEndpointRouteBuilder candidateSessions) =>
        candidateSessions.MapPost(string.Empty, HandleAsync)
            .WithName("CreateSession")
            .WithSummary(
                "Plans an interview session for a candidate, optionally scheduled, shown as \"Session N\" for the candidate. "
                + "Refused for an anonymised candidate (409 InterviewSession.CandidateAnonymised) or a closed position "
                + "(409 InterviewSession.PositionClosed).");

    private static async Task<Results<Created<CreatedSessionResponse>, ProblemHttpResult>> HandleAsync(
        CandidateId candidateId,
        CreateSessionRequest request,
        ICommandHandler<CreateSessionCommand, Result<CreatedSessionResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateSessionCommand(candidateId, request.ScheduledAt), cancellationToken);
        return result.ToHttpResult(created => TypedResults.Created($"/api/staff/sessions/{created.Id}", created));
    }
}
