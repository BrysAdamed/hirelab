using FluentValidation;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Teams;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Users.ReassignUser;

internal sealed record ReassignUserCommand(UserId UserId, Guid TeamId);

internal sealed class ReassignUserValidator : AbstractValidator<ReassignUserCommand>
{
    public ReassignUserValidator()
    {
        RuleFor(command => command.TeamId).NotEmpty();
    }
}

/// <summary>
/// Moves an interviewer to another team with a new security stamp, so their sessions in the old team end within the
/// validation window (ADR 0009). Invites are not touched. Moving an interviewer to their own team changes nothing.
/// </summary>
internal sealed class ReassignUserHandler(IdentityModuleDbContext dbContext, UserManager<User> userManager)
    : ICommandHandler<ReassignUserCommand, Result>
{
    public async Task<Result> HandleAsync(ReassignUserCommand command, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(user => user.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        if (user.Role != StaffRole.Interviewer)
        {
            return UserErrors.NotAnInterviewer;
        }

        var teamId = new TeamId(command.TeamId);
        if (!await dbContext.Teams.AnyAsync(team => team.Id == teamId, cancellationToken))
        {
            return TeamErrors.NotFound;
        }

        if (user.TeamId == teamId)
        {
            return Result.Success();
        }

        user.MoveToTeam(teamId);
        return await userManager.SaveAccessChangeAsync(user);
    }
}
