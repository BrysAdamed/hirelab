namespace HireLab.Modules.Positions.Domain;

/// <summary>
/// What a manager writes about a job position: a title and Markdown sections. Requirements are required; a blank
/// optional section is stored as <c>null</c>.
/// </summary>
internal sealed record JobPositionDetails(
    string Title,
    string? Description,
    string Requirements,
    string? NiceToHaves,
    string? InterviewerNotes)
{
    public const int TitleMaxLength = 200;
    public const int SectionMaxLength = 20_000;

    public JobPositionDetails Normalised() => new(
        Title.Trim(),
        OptionalSection(Description),
        Requirements,
        OptionalSection(NiceToHaves),
        OptionalSection(InterviewerNotes));

    private static string? OptionalSection(string? markdown) => string.IsNullOrWhiteSpace(markdown) ? null : markdown;
}
