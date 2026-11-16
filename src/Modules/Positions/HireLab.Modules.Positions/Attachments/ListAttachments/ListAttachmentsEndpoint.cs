using HireLab.Modules.Positions.Contracts;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Attachments.ListAttachments;

internal static class ListAttachmentsEndpoint
{
    public static RouteHandlerBuilder MapListAttachments(this IEndpointRouteBuilder attachments) =>
        attachments.MapGet(string.Empty, async (
                CandidateId candidateId,
                IQueryHandler<ListAttachmentsQuery, Result<IReadOnlyList<AttachmentResponse>>> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new ListAttachmentsQuery(candidateId), cancellationToken);
                return result.ToHttpResult(TypedResults.Ok);
            })
            .WithName("ListAttachments")
            .WithSummary("Lists a candidate's attachments, oldest upload first.");
}
