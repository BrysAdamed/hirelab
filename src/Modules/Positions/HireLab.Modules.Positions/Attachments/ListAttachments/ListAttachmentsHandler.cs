using HireLab.Modules.Identity.Contracts;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Attachments.ListAttachments;

internal sealed record ListAttachmentsQuery(CandidateId CandidateId);

/// <summary>An attachment on a candidate's profile. <see cref="UploadedByName"/> is <c>null</c> when the uploader is unknown.</summary>
internal sealed record AttachmentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset UploadedAt,
    Guid UploadedById,
    string? UploadedByName);

internal sealed class ListAttachmentsHandler(PositionsDbContext dbContext, IStaffDirectory staffDirectory)
    : IQueryHandler<ListAttachmentsQuery, Result<IReadOnlyList<AttachmentResponse>>>
{
    public async Task<Result<IReadOnlyList<AttachmentResponse>>> HandleAsync(ListAttachmentsQuery query, CancellationToken cancellationToken)
    {
        if (!await dbContext.Candidates.AnyAsync(candidate => candidate.Id == query.CandidateId, cancellationToken))
        {
            return CandidateErrors.NotFound;
        }

        var attachments = await dbContext.Attachments.AsNoTracking()
            .Where(attachment => attachment.CandidateId == query.CandidateId)
            .OrderBy(attachment => attachment.CreatedAt)
            .ThenBy(attachment => attachment.Id)
            .ToListAsync(cancellationToken);
        var uploaders = await staffDirectory.FindDisplayNamesAsync(
            attachments.Select(attachment => attachment.UploadedBy).Distinct().ToList(),
            cancellationToken);

        return attachments
            .Select(attachment => new AttachmentResponse(
                attachment.Id.Value,
                attachment.FileName,
                attachment.ContentType,
                attachment.Size,
                attachment.CreatedAt,
                attachment.UploadedBy.Value,
                uploaders.GetValueOrDefault(attachment.UploadedBy)))
            .ToList();
    }
}
