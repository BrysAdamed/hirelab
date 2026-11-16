using FluentValidation;
using HireLab.Modules.Positions.Attachments.Storage;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.Modules.Positions.Persistence.Configurations;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireLab.Modules.Positions.Attachments.UploadAttachment;

/// <summary>An upload: <see cref="Content"/> must be seekable.</summary>
internal sealed record UploadAttachmentCommand(CandidateId CandidateId, string FileName, long Size, Stream Content)
{
    /// <summary>The file name without any directory part a browser may have sent.</summary>
    public static string FileNameOf(string? uploadedName)
    {
        var name = uploadedName?.Trim() ?? string.Empty;
        return name[(name.LastIndexOfAny(['/', '\\']) + 1)..];
    }
}

internal sealed record UploadedAttachmentResponse(Guid Id, string FileName, string ContentType, long Size);

internal sealed class UploadAttachmentValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentValidator()
    {
        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(Attachment.FileNameMaxLength)
            .Must(name => !name.Any(char.IsControl)).WithMessage("The file name must not contain control characters.");
    }
}

/// <summary>
/// Uploads a file to a candidate's profile (ADR 0005). The checks run first, then the blob is stored, then the row is
/// inserted in a transaction that holds a share lock on the candidate and position rows, so closing the position or
/// anonymising the candidate (which update those rows) can't interleave with the insert. The row takes the
/// candidate's lowest free slot; of two concurrent uploads racing for one slot, the loser retries with the next one.
/// When the row isn't saved the blob is deleted, best effort; the orphan sweep is the safety net.
/// </summary>
internal sealed partial class UploadAttachmentHandler(
    PositionsDbContext dbContext,
    IAttachmentStore store,
    ICurrentUser currentUser,
    ILogger<UploadAttachmentHandler> logger)
    : ICommandHandler<UploadAttachmentCommand, Result<UploadedAttachmentResponse>>
{
    private const int MaxAttempts = 3;

    public async Task<Result<UploadedAttachmentResponse>> HandleAsync(UploadAttachmentCommand command, CancellationToken cancellationToken)
    {
        if (command.Size == 0)
        {
            return AttachmentErrors.Empty;
        }

        if (command.Size > Attachment.MaxSizeInBytes)
        {
            return AttachmentErrors.TooLarge;
        }

        var target = await LoadAsync(command.CandidateId, cancellationToken);
        if (target.IsFailure)
        {
            return target.Error;
        }

        var canUpload = Attachment.EnsureCanUpload(target.Value.Candidate, target.Value.Position, target.Value.TakenSlots);
        if (canUpload.IsFailure)
        {
            return canUpload.Error;
        }

        var type = AttachmentTypeDetector.Detect(command.FileName, command.Content);
        if (type is null)
        {
            return AttachmentErrors.UnsupportedType;
        }

        var file = new UploadedFile(command.FileName, type, command.Size);
        var attachmentId = AttachmentId.New();
        await store.UploadAsync(command.CandidateId, attachmentId, command.Content, type.ContentType, cancellationToken);

        Result<Attachment> saved;
        try
        {
            saved = await SaveAsync(command.CandidateId, attachmentId, file, cancellationToken);
        }
        catch
        {
            await DeleteBlobAsync(command.CandidateId, attachmentId);
            throw;
        }

        if (saved.IsFailure)
        {
            await DeleteBlobAsync(command.CandidateId, attachmentId);
            return saved.Error;
        }

        return new UploadedAttachmentResponse(attachmentId.Value, file.FileName, type.ContentType, file.Size);
    }

    private async Task<Result<Attachment>> SaveAsync(
        CandidateId candidateId,
        AttachmentId attachmentId,
        UploadedFile file,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                SELECT 1 FROM positions.candidates c
                JOIN positions.job_positions p ON p.id = c.job_position_id
                WHERE c.id = {candidateId.Value}
                FOR SHARE
                """,
                cancellationToken);

            var target = await LoadAsync(candidateId, cancellationToken);
            if (target.IsFailure)
            {
                return target.Error;
            }

            var (candidate, position, takenSlots) = target.Value;
            var attachment = Attachment.Upload(attachmentId, candidate, position, takenSlots, file, currentUser.Id);
            if (attachment.IsFailure)
            {
                return attachment;
            }

            dbContext.Attachments.Add(attachment.Value);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return attachment;
            }
            catch (DbUpdateException exception) when (exception.ViolatedConstraint() == AttachmentConfiguration.SlotIndex)
            {
                await transaction.RollbackAsync(cancellationToken);
                dbContext.ChangeTracker.Clear();
                if (attempt == MaxAttempts)
                {
                    return Error.ChangedMeanwhile;
                }
            }
        }
    }

    private async Task<Result<UploadTarget>> LoadAsync(CandidateId candidateId, CancellationToken cancellationToken)
    {
        var candidate = await dbContext.Candidates.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == candidateId, cancellationToken);
        if (candidate is null)
        {
            return CandidateErrors.NotFound;
        }

        var position = await dbContext.JobPositions.AsNoTracking()
            .SingleAsync(position => position.Id == candidate.JobPositionId, cancellationToken);
        var takenSlots = await dbContext.Attachments
            .Where(attachment => attachment.CandidateId == candidateId)
            .Select(attachment => attachment.Slot)
            .ToListAsync(cancellationToken);

        return new UploadTarget(candidate, position, takenSlots);
    }

    private async Task DeleteBlobAsync(CandidateId candidateId, AttachmentId attachmentId)
    {
        try
        {
            await store.DeleteAsync(candidateId, attachmentId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogBlobNotDeleted(logger, exception, candidateId.Value, attachmentId.Value);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The blob of attachment {AttachmentId} of candidate {CandidateId} was not deleted after a failed upload; the sweep will remove it")]
    private static partial void LogBlobNotDeleted(ILogger logger, Exception exception, Guid candidateId, Guid attachmentId);

    private sealed record UploadTarget(Candidate Candidate, JobPosition Position, IReadOnlyCollection<int> TakenSlots);
}
