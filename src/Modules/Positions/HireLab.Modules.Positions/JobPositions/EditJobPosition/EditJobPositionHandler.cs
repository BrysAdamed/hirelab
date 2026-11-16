using HireLab.Modules.Positions.Contracts;
using FluentValidation;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Positions.JobPositions.EditJobPosition;

internal sealed record EditJobPositionCommand(JobPositionId PositionId, JobPositionDetails Details, uint Version);

internal sealed class EditJobPositionValidator : AbstractValidator<EditJobPositionCommand>
{
    public EditJobPositionValidator()
    {
        RuleFor(command => command.Details).SetValidator(new JobPositionDetailsValidator());
    }
}

/// <summary>
/// Replaces an open position's title and sections. The edit is based on <see cref="EditJobPositionCommand.Version"/>:
/// when someone else saved the position since, it is refused with <c>Concurrency.ChangedMeanwhile</c>.
/// </summary>
internal sealed class EditJobPositionHandler(PositionsDbContext dbContext) : ICommandHandler<EditJobPositionCommand, Result>
{
    public async Task<Result> HandleAsync(EditJobPositionCommand command, CancellationToken cancellationToken)
    {
        var position = await dbContext.JobPositions.SingleOrDefaultAsync(position => position.Id == command.PositionId, cancellationToken);
        if (position is null)
        {
            return JobPositionErrors.NotFound;
        }

        dbContext.ExpectVersion(position, command.Version);
        return position.Edit(command.Details);
    }
}
