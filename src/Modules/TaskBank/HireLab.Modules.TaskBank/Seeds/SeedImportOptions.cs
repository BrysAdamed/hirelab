namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>Settings of seed import (ADR 0030), bound from <c>TaskBank</c>.</summary>
internal sealed class SeedImportOptions
{
    public const string Section = "TaskBank";

    /// <summary>Imports the seed packages as system tasks in the migration worker. On by default, in every environment.</summary>
    public bool ImportSeedTasks { get; set; } = true;
}
