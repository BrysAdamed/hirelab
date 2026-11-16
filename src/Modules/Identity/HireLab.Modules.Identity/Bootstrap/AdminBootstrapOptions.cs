namespace HireLab.Modules.Identity.Bootstrap;

/// <summary>Settings of the first admin's bootstrap invite (ADR 0022), bound from <c>AdminBootstrap</c>.</summary>
internal sealed class AdminBootstrapOptions
{
    public const string Section = "AdminBootstrap";

    public const string DefaultDisplayName = "Administrator";

    /// <summary>The email (and login) of the admin the bootstrap invite is for. Required outside Development.</summary>
    public string? Email { get; set; }

    public string DisplayName { get; set; } = DefaultDisplayName;

    /// <summary>
    /// Issues a fresh invite even when one is pending, revoking it. Set for a single manual run of the migration job
    /// (<c>--issue-admin-link</c>), never permanently.
    /// </summary>
    public bool IssueNewLink { get; set; }
}
