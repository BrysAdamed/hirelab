namespace HireLab.Modules.TaskBank.Seeds;

/// <summary>
/// A seed package that can't be imported (ADR 0030). It stops the migration worker, so the API never starts with a bad
/// seed. Its message names the package and the broken rule, never the package's content.
/// </summary>
internal sealed class SeedPackageException : Exception
{
    public SeedPackageException(string message)
        : base(message)
    {
    }

    public SeedPackageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SeedPackageException()
    {
    }
}
