using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace HireLab.Modules.Positions.Attachments.UploadAttachment;

internal static class UploadAttachmentEndpoint
{
    /// <summary>The largest request body accepted: one file at the size limit plus room for the multipart framing.</summary>
    public const long MaxRequestBodySize = Attachment.MaxSizeInBytes + (64 * 1024);

    public static RouteHandlerBuilder MapUploadAttachment(this IEndpointRouteBuilder attachments) =>
        attachments.MapPost(string.Empty, HandleAsync)
            .WithName("UploadAttachment")
            .WithSummary(
                "Uploads a file (multipart form field `file`) to a candidate while their position is open. PDF, DOCX, TXT, MD, "
                + "PNG or JPG, checked by content; at most 10 MB and 10 files per candidate. A larger request body is refused "
                + "with 413.")
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBodySize))
            // The staff group's header-based antiforgery filter still applies; this only turns off the form-field check.
            .DisableAntiforgery();

    private static async Task<Results<Created<UploadedAttachmentResponse>, ProblemHttpResult>> HandleAsync(
        CandidateId candidateId,
        IFormFile file,
        ICommandHandler<UploadAttachmentCommand, Result<UploadedAttachmentResponse>> handler,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var command = new UploadAttachmentCommand(candidateId, UploadAttachmentCommand.FileNameOf(file.FileName), file.Length, content);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult(uploaded =>
            TypedResults.Created($"/api/staff/candidates/{candidateId}/attachments/{uploaded.Id}", uploaded));
    }
}
