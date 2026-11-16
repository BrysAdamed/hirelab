using FluentValidation;
using HireLab.Modules.Identity.Domain;

namespace HireLab.Modules.Identity.Invites.CreateManagerInvite;

/// <summary>Invites a manager either into a new team (<see cref="NewTeamName"/>) or into an existing team (<see cref="TeamId"/>).</summary>
internal sealed record CreateManagerInviteCommand(string Email, string DisplayName, Guid? TeamId, string? NewTeamName)
{
    public override string ToString() => nameof(CreateManagerInviteCommand);
}

internal sealed class CreateManagerInviteValidator : AbstractValidator<CreateManagerInviteCommand>
{
    public CreateManagerInviteValidator()
    {
        RuleFor(command => command.Email).ValidInviteeEmail();
        RuleFor(command => command.DisplayName).ValidInviteeDisplayName();

        RuleFor(command => command.NewTeamName)
            .NotEmpty()
            .When(command => command.TeamId is null)
            .WithMessage("Enter a name for the new team, or choose an existing team.");
        RuleFor(command => command.NewTeamName)
            .Empty()
            .When(command => command.TeamId is not null)
            .WithMessage("Choose either a new team or an existing team, not both.");
        RuleFor(command => command.NewTeamName).MaximumLength(Team.NameMaxLength);
    }
}
