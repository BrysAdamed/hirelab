using System.Text.Json.Serialization;

namespace HireLab.TaskTypes.CodeReview;

/// <summary>What the candidate sees: the pull request's title, its Markdown description and the changed files.</summary>
public sealed record CodeReviewCandidateContent(string Title, string Description, IReadOnlyList<ChangedFile> Files);

/// <summary>
/// One file of the pull request. <see cref="Original"/> is the text before the change and <see cref="Modified"/> the
/// text after it: an added file has no original and a deleted file has no modified text.
/// <see cref="Language"/> is one of <see cref="CodeLanguages.All"/>.
/// </summary>
public sealed record ChangedFile(string Path, string Language, string? Original = null, string? Modified = null)
{
    /// <summary>The text of <paramref name="side"/>, or <c>null</c> when the file has no such side.</summary>
    public string? TextOf(DiffSide side) => side switch
    {
        DiffSide.Original => Original,
        DiffSide.Modified => Modified,
        _ => null,
    };
}

/// <summary>A side of a changed file's diff. Serialised as <c>original</c> or <c>modified</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DiffSide>))]
public enum DiffSide
{
    [JsonStringEnumMemberName("original")]
    Original,

    [JsonStringEnumMemberName("modified")]
    Modified,
}

/// <summary>The languages a changed file may use. Each maps to a Monaco grammar, so a new one is a code change (ADR 0032).</summary>
public static class CodeLanguages
{
    public static IReadOnlyList<string> All { get; } =
    [
        "csharp",
        "typescript",
        "javascript",
        "html",
        "css",
        "json",
        "sql",
        "xml",
        "yaml",
        "markdown",
        "plaintext",
    ];
}
