using FluentValidation;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.Modules.Positions.Persistence.Configurations;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Candidates.AddCandidate;

internal sealed record AddCandidateCommand(JobPositionId PositionId, CandidateDetails Details);

internal sealed record AddedCandidateResponse(Guid Id, int Number);

internal sealed class AddCandidateValidator : AbstractValidator<AddCandidateCommand>
{
    public AddCandidateValidator()
    {
        RuleFor(command => command.Details).SetValidator(new CandidateDetailsValidator());
    }
}

/// <summary>
/// Adds a candidate to an open position with the position's next number. Taking a number changes the position, so of
/// two concurrent adds one loses the race (on the position's version or on the unique number index); the loser
/// starts again with a fresh position and takes the next number, and fails with <c>Concurrency.ChangedMeanwhile</c>
/// only after <see cref="MaxAttempts"/> lost races. A position closed meanwhile refuses the retry.
/// </summary>
internal sealed class AddCandidateHandler(PositionsDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<AddCandidateCommand, Result<AddedCandidateResponse>>
{
    private const int MaxAttempts = 3;

    public async Task<Result<AddedCandidateResponse>> HandleAsync(AddCandidateCommand command, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var position = await dbContext.JobPositions.SingleOrDefaultAsync(position => position.Id == command.PositionId, cancellationToken);
            if (position is null)
            {
                return JobPositionErrors.NotFound;
            }

            var added = position.AddCandidate(command.Details, currentUser.Id);
            if (added.IsFailure)
            {
                return added.Error;
            }

            dbContext.Candidates.Add(added.Value);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return new AddedCandidateResponse(added.Value.Id.Value, added.Value.Number);
            }
            catch (DbUpdateException exception) when (IsLostRace(exception))
            {
                dbContext.ChangeTracker.Clear();
                if (attempt == MaxAttempts)
                {
                    return Error.ChangedMeanwhile;
                }
            }
        }
    }

    private static bool IsLostRace(DbUpdateException exception) =>
        exception is DbUpdateConcurrencyException || exception.ViolatedConstraint() == CandidateConfiguration.NumberIndex;
}
