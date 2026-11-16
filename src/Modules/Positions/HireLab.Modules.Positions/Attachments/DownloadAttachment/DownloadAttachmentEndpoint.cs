using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Http;
using HireLab.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace HireLab.Modules.Positions.Attachments.DownloadAttachment;

internal static class DownloadAttachmentEndpoint
{
    public static RouteHandlerBuilder MapDownloadAttachment(this IEndpointRouteBuilder attachments) =>
        attachments.MapGet("/{attachmentId:guid}", HandleAsync)
            .WithName("DownloadAttachment")
            .WithSummary(
                "Downloads an attachment's content with its stored content type, always as a download "
                + "(Content-Disposition: attachment with the original file name) and never sniffed or cached.")
            .Produces<Stream>(StatusCodes.Status200OK, "application/octet-stream");

    private static async Task<Results<FileStreamHttpResult, ProblemHttpResult>> HandleAsync(
        CandidateId candidateId,
        AttachmentId attachmentId,
        HttpResponse response,
        IQueryHandler<DownloadAttachmentQuery, Result<AttachmentDownload>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DownloadAttachmentQuery(candidateId, attachmentId), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        response.Headers.CacheControl = "private, no-store";
        var download = result.Value;
        return TypedResults.Stream(download.Content, download.ContentType, download.FileName);
    }
}
