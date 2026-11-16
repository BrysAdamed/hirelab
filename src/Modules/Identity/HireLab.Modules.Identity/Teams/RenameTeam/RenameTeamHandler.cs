using FluentValidation;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Teams.RenameTeam;

internal sealed record RenameTeamCommand(TeamId TeamId, string Name);

internal sealed class RenameTeamValidator : AbstractValidator<RenameTeamCommand>
{
    public RenameTeamValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(Team.NameMaxLength);
    }
}

internal sealed class RenameTeamHandler(IdentityModuleDbContext dbContext) : ICommandHandler<RenameTeamCommand, Result>
{
    public async Task<Result> HandleAsync(RenameTeamCommand command, CancellationToken cancellationToken)
    {
        var team = await dbContext.Teams.SingleOrDefaultAsync(team => team.Id == command.TeamId, cancellationToken);
        if (team is null)
        {
            return TeamErrors.NotFound;
        }

        team.Rename(command.Name);
        return Result.Success();
    }
}
