using FluentValidation;
using HireLab.Modules.Identity.Domain;

namespace HireLab.Modules.Identity.Invites;

internal static class InviteeValidation
{
    public static IRuleBuilderOptions<T, string> ValidInviteeEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Invite.EmailMaxLength).EmailAddress();

    public static IRuleBuilderOptions<T, string> ValidInviteeDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(User.DisplayNameMaxLength);
}
