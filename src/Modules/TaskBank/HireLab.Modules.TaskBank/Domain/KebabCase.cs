using System.Text.RegularExpressions;

namespace HireLab.Modules.TaskBank.Domain;

internal static partial class KebabCase
{
    public static bool IsMatch(string? value) => value is not null && Pattern().IsMatch(value);

    [GeneratedRegex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
