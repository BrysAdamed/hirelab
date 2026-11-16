using FluentValidation;
using HireLab.Modules.Positions.Domain;
using HireLab.Modules.Positions.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Positions.JobPositions.CreateJobPosition;

internal sealed record CreateJobPositionCommand(JobPositionDetails Details);

internal sealed record CreatedJobPositionResponse(Guid Id);

internal sealed class CreateJobPositionValidator : AbstractValidator<CreateJobPositionCommand>
{
    public CreateJobPositionValidator()
    {
        RuleFor(command => command.Details).SetValidator(new JobPositionDetailsValidator());
    }
}

/// <summary>Creates an open job position in the manager's team.</summary>
internal sealed class CreateJobPositionHandler(PositionsDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreateJobPositionCommand, Result<CreatedJobPositionResponse>>
{
    public Task<Result<CreatedJobPositionResponse>> HandleAsync(CreateJobPositionCommand command, CancellationToken cancellationToken)
    {
        var teamId = currentUser.TeamId ?? throw new InvalidOperationException("A manager always belongs to a team.");
        var position = JobPosition.Create(teamId, command.Details, currentUser.Id);
        dbContext.JobPositions.Add(position);

        return Task.FromResult<Result<CreatedJobPositionResponse>>(new CreatedJobPositionResponse(position.Id.Value));
    }
}
