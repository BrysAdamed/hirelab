using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Attachments.DeleteAttachment;

internal static class DeleteAttachmentEndpoint
{
    public static RouteHandlerBuilder MapDeleteAttachment(this IEndpointRouteBuilder attachments) =>
        attachments.MapDelete("/{attachmentId:guid}", async (
                CandidateId candidateId,
                AttachmentId attachmentId,
                ICommandHandler<DeleteAttachmentCommand, Result> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new DeleteAttachmentCommand(candidateId, attachmentId), cancellationToken);
                return result.ToHttpResult();
            })
            .WithName("DeleteAttachment")
            .WithSummary("Deletes an attachment for good while the candidate's position is open (409 JobPosition.Closed otherwise).");
}
