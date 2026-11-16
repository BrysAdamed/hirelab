using System.Text.RegularExpressions;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>The rules for a task version's tags: lowercase, trimmed, <c>[a-z0-9:+#.-]</c>, at most 32 characters and 10 tags.</summary>
internal static partial class TaskTags
{
    public const int MaxCount = 10;
    public const int MaxLength = 32;

    public static string Normalise(string? tag) => (tag ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValid(string normalisedTag) =>
        normalisedTag.Length is > 0 and <= MaxLength && Pattern().IsMatch(normalisedTag);

    [GeneratedRegex("^[a-z0-9:+#.-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
