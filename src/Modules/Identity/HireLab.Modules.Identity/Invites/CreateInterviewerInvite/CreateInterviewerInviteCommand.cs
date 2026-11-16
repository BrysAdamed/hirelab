using FluentValidation;

namespace HireLab.Modules.Identity.Invites.CreateInterviewerInvite;

internal sealed record CreateInterviewerInviteCommand(string Email, string DisplayName)
{
    public override string ToString() => nameof(CreateInterviewerInviteCommand);
}

internal sealed class CreateInterviewerInviteValidator : AbstractValidator<CreateInterviewerInviteCommand>
{
    public CreateInterviewerInviteValidator()
    {
        RuleFor(command => command.Email).ValidInviteeEmail();
        RuleFor(command => command.DisplayName).ValidInviteeDisplayName();
    }
}
