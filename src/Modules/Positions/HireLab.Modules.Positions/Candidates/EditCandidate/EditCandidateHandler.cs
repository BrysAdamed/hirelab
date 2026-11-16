using FluentValidation;
using HireLab.Modules.Positions.Contracts;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.Candidates.EditCandidate;

internal sealed record EditCandidateCommand(CandidateId CandidateId, CandidateDetails Details, uint Version);

internal sealed class EditCandidateValidator : AbstractValidator<EditCandidateCommand>
{
    public EditCandidateValidator()
    {
        RuleFor(command => command.Details).SetValidator(new CandidateDetailsValidator());
    }
}

/// <summary>
/// Replaces a candidate's contact details while their position is open. The edit is based on
/// <see cref="EditCandidateCommand.Version"/>: when someone else saved the candidate since, it is refused with
/// <c>Concurrency.ChangedMeanwhile</c>.
/// </summary>
internal sealed class EditCandidateHandler(PositionsDbContext dbContext) : ICommandHandler<EditCandidateCommand, Result>
{
    public async Task<Result> HandleAsync(EditCandidateCommand command, CancellationToken cancellationToken)
    {
        var candidate = await dbContext.Candidates.SingleOrDefaultAsync(candidate => candidate.Id == command.CandidateId, cancellationToken);
        if (candidate is null)
        {
            return CandidateErrors.NotFound;
        }

        var position = await dbContext.JobPositions.SingleAsync(position => position.Id == candidate.JobPositionId, cancellationToken);
        dbContext.ExpectVersion(candidate, command.Version);
        return candidate.Edit(command.Details, position);
    }
}
