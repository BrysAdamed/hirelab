using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Invites;

internal sealed record InviteResponse(
    Guid Id,
    string Email,
    string DisplayName,
    StaffRole Role,
    InviteTeamResponse? Team,
    DateTimeOffset ExpiresAt,
    OneTimeLinkState State)
{
    public static InviteResponse From(Invite invite, string? existingTeamName, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(invite);

        var team = (invite.TeamId, invite.NewTeamName) switch
        {
            (null, { } newTeamName) => new InviteTeamResponse(null, newTeamName, IsNew: true),
            ({ } teamId, _) => new InviteTeamResponse(teamId.Value, existingTeamName ?? string.Empty, IsNew: false),
            _ => null,
        };

        return new InviteResponse(
            invite.Id.Value,
            invite.Email,
            invite.DisplayName,
            invite.Role,
            team,
            invite.ExpiresAt,
            invite.StateAt(now));
    }
}

/// <summary>The invite's team: an existing one, or a new one (no id yet) that redeeming the invite creates.</summary>
internal sealed record InviteTeamResponse(Guid? Id, string Name, bool IsNew);

/// <summary>A new invite and the path of its link, shown once: only a hash of the token is stored.</summary>
internal sealed record IssuedInviteResponse(InviteResponse Invite, string LinkPath);
