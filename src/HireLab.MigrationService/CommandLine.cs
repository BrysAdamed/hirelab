using HireLab.Modules.Identity;

namespace HireLab.MigrationService;

internal static class CommandLine
{
    /// <summary>
    /// Issues a fresh admin link in this run (ADR 0022): an invite replacing a pending one, or a reset link when the admin
    /// exists.
    /// </summary>
    public const string IssueAdminLink = "--issue-admin-link";

    /// <summary>Turns the job's own switches into configuration settings, so they bind like every other setting.</summary>
    public static string[] ToConfigurationArgs(IEnumerable<string> args) =>
        args.Select(arg => arg == IssueAdminLink ? $"--{IdentityModule.IssueAdminLinkSetting}=true" : arg).ToArray();
}
