using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Candidates.AnonymiseCandidate;

internal sealed record AnonymiseCandidateRequest(uint Version);

internal static class AnonymiseCandidateEndpoint
{
    public static RouteHandlerBuilder MapAnonymiseCandidate(this IEndpointRouteBuilder candidates) =>
        candidates.MapPost("/{candidateId:guid}/anonymise", async (
                CandidateId candidateId,
                AnonymiseCandidateRequest request,
                ICommandHandler<AnonymiseCandidateCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new AnonymiseCandidateCommand(candidateId, request.Version), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("AnonymiseCandidate")
            .WithSummary(
                "Anonymises a candidate of an open position for good: contact details are cleared and attachments "
                + "deleted. Refused while their interview session isn't finished (409 Candidate.UnfinishedSessions) and "
                + "when already anonymised (409 Candidate.Anonymised). Send the version it was read with (409 "
                + "Concurrency.ChangedMeanwhile when it changed since).");
}
