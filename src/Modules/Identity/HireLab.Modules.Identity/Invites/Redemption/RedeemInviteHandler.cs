using FluentValidation;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.Persistence.Configurations;
using HireLab.SharedKernel.Handlers;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Results;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Invites.Redemption;

internal sealed record RedeemInviteCommand(string Token, string Password)
{
    public override string ToString() => nameof(RedeemInviteCommand);
}

internal sealed class RedeemInviteValidator : AbstractValidator<RedeemInviteCommand>
{
    public RedeemInviteValidator()
    {
        RuleFor(command => command.Token).NotEmpty().MaximumLength(OneTimeToken.MaxLength);
        RuleFor(command => command.Password).NotEmpty();
    }
}

/// <summary>
/// Creates the invited user (and, for a new-team manager invite, the team), marks the invite used and signs the user in.
/// The invite, team and user are saved together; the database indexes stop a concurrent redemption or a second manager.
/// </summary>
internal sealed class RedeemInviteHandler(
    IdentityModuleDbContext dbContext,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    TimeProvider timeProvider)
    : ICommandHandler<RedeemInviteCommand, Result<UserId>>
{
    public async Task<Result<UserId>> HandleAsync(RedeemInviteCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var found = await dbContext.Invites.FindUsableAsync(command.Token, now, InviteErrors.LinkNotFound, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        var invite = found.Value;
        if (await userManager.FindByEmailAsync(invite.Email) is not null)
        {
            return InviteErrors.EmailInUse;
        }

        var teamId = invite.TeamId;
        if (invite.NewTeamName is { } newTeamName)
        {
            var team = Team.Create(newTeamName);
            dbContext.Teams.Add(team);
            teamId = team.Id;
        }
        else if (invite.Role == StaffRole.Manager && await dbContext.HasActiveManagerAsync(teamId!.Value, cancellationToken))
        {
            return InviteErrors.TeamHasManager;
        }
        else if (invite.Role == StaffRole.Admin && await dbContext.Users.AnyAsync(user => user.Role == StaffRole.Admin, cancellationToken))
        {
            return InviteErrors.AdminExists;
        }

        var used = invite.Use(now);
        if (used.IsFailure)
        {
            return used.Error;
        }

        var user = invite.CreateUser(teamId);
        var created = await CreateAsync(user, command.Password);
        if (created.IsFailure)
        {
            return created.Error;
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return user.Id;
    }

    private async Task<Result> CreateAsync(User user, string password)
    {
        IdentityResult created;
        try
        {
            created = await userManager.CreateAsync(user, password);
        }
        catch (DbUpdateConcurrencyException)
        {
            return InviteErrors.AlreadyUsed;
        }
        catch (DbUpdateException exception) when (ConflictFor(exception.ViolatedConstraint()) is { } conflict)
        {
            return conflict;
        }

        if (created.Succeeded)
        {
            return Result.Success();
        }

        if (created.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return InviteErrors.EmailInUse;
        }

        return InviteErrors.PasswordRejected(created.Errors.Select(error => error.Description));
    }

    private static Error? ConflictFor(string? constraint) => constraint switch
    {
        UserConfiguration.OneActiveManagerPerTeamIndex => InviteErrors.TeamHasManager,
        UserConfiguration.SingleAdminIndex => InviteErrors.AdminExists,
        _ => null,
    };
}
