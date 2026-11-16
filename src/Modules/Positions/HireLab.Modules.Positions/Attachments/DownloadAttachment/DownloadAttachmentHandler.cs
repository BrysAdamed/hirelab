using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireLab.Modules.Positions.Attachments.DownloadAttachment;

internal sealed record DownloadAttachmentQuery(CandidateId CandidateId, AttachmentId AttachmentId);

/// <summary>An attachment's content, to stream to the client. The caller disposes <see cref="Content"/>.</summary>
internal sealed record AttachmentDownload(string FileName, string ContentType, Stream Content);

internal sealed partial class DownloadAttachmentHandler(
    PositionsDbContext dbContext,
    IAttachmentStore store,
    ILogger<DownloadAttachmentHandler> logger)
    : IQueryHandler<DownloadAttachmentQuery, Result<AttachmentDownload>>
{
    public async Task<Result<AttachmentDownload>> HandleAsync(DownloadAttachmentQuery query, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.Attachments.AsNoTracking()
            .SingleOrDefaultAsync(
                attachment => attachment.Id == query.AttachmentId && attachment.CandidateId == query.CandidateId,
                cancellationToken);
        if (attachment is null)
        {
            return AttachmentErrors.NotFound;
        }

        var content = await store.OpenReadAsync(attachment.CandidateId, attachment.Id, cancellationToken);
        if (content is null)
        {
            LogBlobMissing(logger, attachment.CandidateId.Value, attachment.Id.Value);
            return AttachmentErrors.NotFound;
        }

        return new AttachmentDownload(attachment.FileName, attachment.ContentType, content);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The blob of attachment {AttachmentId} of candidate {CandidateId} is missing")]
    private static partial void LogBlobMissing(ILogger logger, Guid candidateId, Guid attachmentId);
}
