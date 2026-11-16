using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.CreateManagerInvite;

internal sealed class CreateManagerInviteHandler(
    IdentityModuleDbContext dbContext,
    InviteIssuer issuer,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CreateManagerInviteCommand, Result<IssuedInviteResponse>>
{
    public async Task<Result<IssuedInviteResponse>> HandleAsync(CreateManagerInviteCommand command, CancellationToken cancellationToken)
    {
        string? existingTeamName = null;
        if (command.TeamId is { } id)
        {
            var teamId = new TeamId(id);
            var team = await dbContext.Teams.AsNoTracking().SingleOrDefaultAsync(team => team.Id == teamId, cancellationToken);
            if (team is null)
            {
                return InviteErrors.TeamNotFound;
            }

            if (await dbContext.HasActiveManagerAsync(teamId, cancellationToken))
            {
                return InviteErrors.TeamHasManager;
            }

            existingTeamName = team.Name;
        }

        var invitedBy = currentUser.Id;
        var issued = await issuer.IssueAsync(
            command.Email,
            command.DisplayName,
            StaffScope.Managers,
            (invitee, token, now) => command.TeamId is { } teamId
                ? Invite.ForManagerOfTeam(invitee, new TeamId(teamId), invitedBy, token, now)
                : Invite.ForManagerOfNewTeam(invitee, command.NewTeamName!, invitedBy, token, now),
            cancellationToken);

        if (issued.IsFailure)
        {
            return issued.Error;
        }

        return new IssuedInviteResponse(
            InviteResponse.From(issued.Value.Link, existingTeamName, timeProvider.GetUtcNow()),
            issued.Value.LinkPath);
    }
}
