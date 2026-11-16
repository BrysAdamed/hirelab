using HireLab.Modules.Identity;

namespace HireLab.MigrationService;

internal static class CommandLine
{
    /// <summary>Issues a fresh admin invite link in this run, revoking a pending one (ADR 0022).</summary>
    public const string IssueAdminLink = "--issue-admin-link";

    /// <summary>Turns the job's own switches into configuration settings, so they bind like every other setting.</summary>
    public static string[] ToConfigurationArgs(IEnumerable<string> args) =>
        args.Select(arg => arg == IssueAdminLink ? $"--{IdentityModule.IssueAdminLinkSetting}=true" : arg).ToArray();
}
