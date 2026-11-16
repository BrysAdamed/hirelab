using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.CreateInterviewerInvite;

internal sealed class CreateInterviewerInviteHandler(
    IdentityModuleDbContext dbContext,
    InviteIssuer issuer,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<CreateInterviewerInviteCommand, Result<IssuedInviteResponse>>
{
    public async Task<Result<IssuedInviteResponse>> HandleAsync(CreateInterviewerInviteCommand command, CancellationToken cancellationToken)
    {
        var invitedBy = currentUser.Id;
        var teamId = currentUser.TeamId ?? throw new InvalidOperationException("A manager always has a team.");

        var issued = await issuer.IssueAsync(
            command.Email,
            command.DisplayName,
            (invitee, token, now) => Invite.ForInterviewer(invitee, teamId, invitedBy, token, now),
            cancellationToken);

        if (issued.IsFailure)
        {
            return issued.Error;
        }

        var teamName = await dbContext.Teams.Where(team => team.Id == teamId).Select(team => team.Name).SingleAsync(cancellationToken);

        return new IssuedInviteResponse(
            InviteResponse.From(issued.Value.Invite, teamName, timeProvider.GetUtcNow()),
            InviteLinks.PathFor(issued.Value.Token));
    }
}
