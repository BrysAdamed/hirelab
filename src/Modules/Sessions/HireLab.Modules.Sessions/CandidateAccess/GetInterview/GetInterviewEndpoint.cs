using System.Security.Claims;
using HireLab.Modules.Sessions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Sessions.CandidateAccess.GetInterview;

internal static class GetInterviewEndpoint
{
    public static RouteHandlerBuilder MapGetInterview(this IEndpointRouteBuilder candidate) =>
        candidate.MapGet("/session", HandleAsync)
            .WithName("GetInterview")
            .WithSummary(
                "Gets the candidate's interview from the candidate cookie: the waiting page while the session is planned, the "
                + "current task with its candidate content while one is current, and the closing screen after the last task.");

    private static async Task<Results<Ok<InterviewResponse>, ProblemHttpResult>> HandleAsync(
        ClaimsPrincipal user,
        IQueryHandler<GetInterviewQuery, Result<InterviewResponse>> handler,
        CancellationToken cancellationToken)
    {
        if (CandidateCookie.From(user) is not { } cookie)
        {
            return CandidateAccessErrors.InvalidLink.ToProblem();
        }

        var result = await handler.HandleAsync(new GetInterviewQuery(cookie), cancellationToken);
        return result.ToHttpResult(TypedResults.Ok);
    }
}
