using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Invites;
using HireLab.Modules.Identity.OneTimeLinks;
using HireLab.Modules.Identity.Persistence;
using HireLab.Modules.Identity.ResetLinks;
using HireLab.Modules.Identity.Users;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Bootstrap;

/// <summary>
/// Makes sure the single admin can come to exist (ADR 0022): while there is no admin, the migration job issues a
/// bootstrap invite and writes its link to the log. A manual run with <see cref="AdminBootstrapOptions.IssueNewLink"/>
/// replaces a pending invite or, once the admin exists, issues a reset link for them. Issuing an admin invite revokes the
/// open ones in the same transaction, so a run that can't issue keeps the pending invite.
/// </summary>
internal sealed partial class AdminBootstrap(
    IdentityModuleDbContext dbContext,
    InviteIssuer inviteIssuer,
    ResetLinkIssuer resetLinkIssuer,
    IOptions<AdminBootstrapOptions> options,
    IOptions<StaffAppOptions> staffApp,
    TimeProvider timeProvider,
    ILogger<AdminBootstrap> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var adminId = await dbContext.Users
            .Where(user => user.Role == StaffRole.Admin)
            .Select(user => (UserId?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (adminId is { } existingAdmin)
        {
            if (settings.IssueNewLink)
            {
                await IssueResetLinkAsync(existingAdmin, cancellationToken);
            }
            else
            {
                LogAdminExists();
            }

            return;
        }

        if (!settings.IssueNewLink)
        {
            var pendingUntil = await dbContext.Invites
                .WherePendingAt(timeProvider.GetUtcNow())
                .Where(invite => invite.Role == StaffRole.Admin)
                .Select(invite => (DateTimeOffset?)invite.ExpiresAt)
                .MaxAsync(cancellationToken);

            if (pendingUntil is { } expiresAt)
            {
                LogInvitePending(expiresAt);
                return;
            }
        }

        await IssueAsync(settings, cancellationToken);
    }

    private async Task IssueAsync(AdminBootstrapOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.Email))
        {
            throw new InvalidOperationException(
                $"{AdminBootstrapOptions.Section}:{nameof(AdminBootstrapOptions.Email)} is not set, so no admin invite can be issued.");
        }

        var issued = await inviteIssuer.IssueAsync(settings.Email, settings.DisplayName, StaffScope.Everyone, Invite.ForAdmin, cancellationToken);
        if (issued.IsFailure)
        {
            throw new InvalidOperationException($"The admin invite could not be issued: {issued.Error.Code}. {issued.Error.Description}");
        }

        var link = staffApp.Value.LinkTo(issued.Value.LinkPath);
        LogInviteLink(issued.Value.Link.ExpiresAt, link);
    }

    private async Task IssueResetLinkAsync(UserId adminId, CancellationToken cancellationToken)
    {
        var issued = await resetLinkIssuer.IssueAsync(adminId, issuedBy: null, cancellationToken);
        if (issued.IsFailure)
        {
            throw new InvalidOperationException($"The admin's reset link could not be issued: {issued.Error.Code}. {issued.Error.Description}");
        }

        LogResetLink(issued.Value.Link.ExpiresAt, staffApp.Value.LinkTo(issued.Value.LinkPath));
    }

    [LoggerMessage(Level = LogLevel.Debug,Message = "An admin exists, so no admin invite is needed")]
    private partial void LogAdminExists();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "No admin exists yet and an admin invite is pending until {ExpiresAt}. Run the job with --issue-admin-link to replace it")]
    private partial void LogInvitePending(DateTimeOffset expiresAt);

    // This and LogResetLink are the only log lines allowed to carry a link (ADR 0007, ADR 0022): with no email
    // delivery, the job log is how the admin's link reaches a person.
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No admin exists yet. Open this one-time admin invite link before {ExpiresAt} to create the admin: {AdminInviteLink}")]
    private partial void LogInviteLink(DateTimeOffset expiresAt, Uri adminInviteLink);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The admin exists. Open this one-time reset link before {ExpiresAt} to set the admin's password: {AdminResetLink}")]
    private partial void LogResetLink(DateTimeOffset expiresAt, Uri adminResetLink);
}
