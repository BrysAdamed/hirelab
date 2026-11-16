using System.Reflection;

namespace HireLab.Tasks;

/// <summary>
/// The seed packages (ADR 0030): every file under <c>src/Tasks/&lt;slug&gt;/v&lt;N&gt;/</c>, embedded in this assembly as a
/// resource named <see cref="ResourcePrefix"/> plus its path below <c>src/Tasks</c>. Only the migration worker and
/// tests reference this project.
/// </summary>
public static class SeedTaskPackages
{
    /// <summary>The prefix of every seed package file's resource name.</summary>
    public const string ResourcePrefix = "seed/";

    /// <summary>The assembly the seed packages are embedded in.</summary>
    public static Assembly Assembly => typeof(SeedTaskPackages).Assembly;
}
