using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.TaskTypes.CodeReview;

/// <summary>
/// A pull-request-like review (ADR 0032). The candidate content is the pull request with its changed files, the staff
/// content is the expected findings and the response is the candidate's review comments, summary and verdict.
/// Expected findings are checked against the changed files; review comment anchors are not. It suggests no criterion
/// scores. A side's lines are counted as Monaco counts them: line breaks plus one, so a trailing line break adds an
/// empty last line.
/// </summary>
public sealed partial class CodeReviewTaskType : TaskType<CodeReviewCandidateContent, CodeReviewStaffContent, CodeReviewResponse>
{
    public const string TaskTypeKey = "code-review";

    public const int TitleMaxLength = 200;

    public const int DescriptionMaxLength = 20_000;

    public const int MinFiles = 1;

    public const int MaxFiles = 20;

    public const int PathMaxLength = 300;

    public const int SideMaxBytes = 200 * 1024;

    public const int MaxExpectedFindings = 50;

    public const int MaxComments = 200;

    public const int BodyMaxLength = 5_000;

    public const int SummaryMaxLength = 20_000;

    private const string Files = "candidateContent.files";

    private const string ExpectedFindings = "staffContent.expectedFindings";

    private const string Comments = "response.comments";

    public override string Key => TaskTypeKey;

    public override int ContentSchemaVersion => 1;

    public override int ResponseSchemaVersion => 1;

    public override Result ValidateContent(TaskContent<CodeReviewCandidateContent, CodeReviewStaffContent> content)
    {
        var errors = new Errors();
        var files = ValidateCandidateContent(content.Candidate, errors);
        ValidateExpectedFindings(content.Staff.ExpectedFindings, files, errors);
        return errors.ToResult();
    }

    public override Result ValidateResponse(CodeReviewResponse response)
    {
        var errors = new Errors();
        ValidateComments(response.Comments, errors);

        if (response.Summary.Length > SummaryMaxLength)
        {
            errors.Add("response.summary", Invariant($"Keep the summary to at most {SummaryMaxLength:N0} characters."));
        }

        if (response.Verdict is { } verdict && !Enum.IsDefined(verdict))
        {
            errors.Add("response.verdict", "Choose Approve, Request changes or Comment.");
        }

        return errors.ToResult();
    }

    private static Dictionary<string, ChangedFile> ValidateCandidateContent(CodeReviewCandidateContent candidate, Errors errors)
    {
        if (string.IsNullOrWhiteSpace(candidate.Title))
        {
            errors.Add("candidateContent.title", "Enter a title.");
        }
        else if (candidate.Title.Length > TitleMaxLength)
        {
            errors.Add("candidateContent.title", Invariant($"Keep the title to at most {TitleMaxLength:N0} characters."));
        }

        if (candidate.Description.Length > DescriptionMaxLength)
        {
            errors.Add("candidateContent.description", Invariant($"Keep the description to at most {DescriptionMaxLength:N0} characters."));
        }

        if (candidate.Files.Count is < MinFiles or > MaxFiles)
        {
            errors.Add(Files, $"Add {MinFiles} to {MaxFiles} changed files.");
        }

        var files = new Dictionary<string, ChangedFile>(StringComparer.Ordinal);
        for (var index = 0; index < candidate.Files.Count; index++)
        {
            ValidateFile(candidate.Files[index], $"{Files}[{index}]", files, errors);
        }

        return files;
    }

    private static void ValidateFile(ChangedFile? file, string field, Dictionary<string, ChangedFile> files, Errors errors)
    {
        if (file is null)
        {
            errors.Add(field, "Define the changed file.");
            return;
        }

        if (string.IsNullOrWhiteSpace(file.Path))
        {
            errors.Add($"{field}.path", "Enter a path.");
        }
        else if (file.Path.Length > PathMaxLength)
        {
            errors.Add($"{field}.path", $"Keep the path to at most {PathMaxLength} characters.");
        }
        else if (!files.TryAdd(file.Path, file))
        {
            errors.Add($"{field}.path", "Another changed file already uses this path.");
        }

        if (!CodeLanguages.All.Contains(file.Language, StringComparer.Ordinal))
        {
            errors.Add($"{field}.language", $"Choose one of the languages: {string.Join(", ", CodeLanguages.All)}.");
        }

        if (file.Original is null && file.Modified is null)
        {
            errors.Add(field, "Enter the original text, the modified text or both.");
        }

        ValidateSideSize(file.Original, $"{field}.original", errors);
        ValidateSideSize(file.Modified, $"{field}.modified", errors);
    }

    private static void ValidateSideSize(string? text, string field, Errors errors)
    {
        if (text is not null && Encoding.UTF8.GetByteCount(text) > SideMaxBytes)
        {
            errors.Add(field, $"Keep the text to at most {SideMaxBytes / 1024} KB.");
        }
    }

    private static void ValidateExpectedFindings(IReadOnlyList<ExpectedFinding> findings, Dictionary<string, ChangedFile> files, Errors errors)
    {
        if (findings.Count > MaxExpectedFindings)
        {
            errors.Add(ExpectedFindings, $"Add at most {MaxExpectedFindings} expected findings.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < findings.Count; index++)
        {
            var field = $"{ExpectedFindings}[{index}]";
            var finding = findings[index];
            if (finding is null)
            {
                errors.Add(field, "Define the expected finding.");
                continue;
            }

            if (!KebabCase().IsMatch(finding.Id))
            {
                errors.Add($"{field}.id", "Use a kebab-case id, for example 'missing-null-check'.");
            }
            else if (!ids.Add(finding.Id))
            {
                errors.Add($"{field}.id", "Another expected finding already uses this id.");
            }

            ValidateAnchor(finding, files, field, errors);

            if (string.IsNullOrWhiteSpace(finding.Title))
            {
                errors.Add($"{field}.title", "Enter a title.");
            }
            else if (finding.Title.Length > TitleMaxLength)
            {
                errors.Add($"{field}.title", Invariant($"Keep the title to at most {TitleMaxLength:N0} characters."));
            }

            if (finding.Description.Length > DescriptionMaxLength)
            {
                errors.Add($"{field}.description", Invariant($"Keep the description to at most {DescriptionMaxLength:N0} characters."));
            }

            if (!Enum.IsDefined(finding.Severity))
            {
                errors.Add($"{field}.severity", "Choose Critical, Major, Minor or Nit.");
            }
        }
    }

    private static void ValidateAnchor(ExpectedFinding finding, Dictionary<string, ChangedFile> files, string field, Errors errors)
    {
        var linesValid = ValidateLineRange(finding.StartLine, finding.EndLine, field, errors);

        if (!files.TryGetValue(finding.File, out var file))
        {
            errors.Add($"{field}.file", "Choose the path of one of the changed files.");
            return;
        }

        if (!Enum.IsDefined(finding.Side))
        {
            errors.Add($"{field}.side", "Choose the original or the modified side.");
            return;
        }

        if (file.TextOf(finding.Side) is not { } text)
        {
            errors.Add($"{field}.side", finding.Side == DiffSide.Original
                ? "The file has no original text; it is an added file."
                : "The file has no modified text; it is a deleted file.");
            return;
        }

        var lineCount = LineCount(text);
        if (linesValid && finding.EndLine > lineCount)
        {
            errors.Add($"{field}.endLine", $"The {SideName(finding.Side)} side has {lineCount} lines.");
        }
    }

    private static void ValidateComments(IReadOnlyList<ReviewComment> comments, Errors errors)
    {
        if (comments.Count > MaxComments)
        {
            errors.Add(Comments, $"Add at most {MaxComments} review comments.");
        }

        var ids = new HashSet<Guid>();
        for (var index = 0; index < comments.Count; index++)
        {
            var field = $"{Comments}[{index}]";
            var comment = comments[index];
            if (comment is null)
            {
                errors.Add(field, "Define the review comment.");
                continue;
            }

            if (comment.Id == Guid.Empty)
            {
                errors.Add($"{field}.id", "Use a non-empty id.");
            }
            else if (!ids.Add(comment.Id))
            {
                errors.Add($"{field}.id", "Another review comment already uses this id.");
            }

            if (string.IsNullOrWhiteSpace(comment.File))
            {
                errors.Add($"{field}.file", "Enter the path of the file.");
            }
            else if (comment.File.Length > PathMaxLength)
            {
                errors.Add($"{field}.file", $"Keep the path to at most {PathMaxLength} characters.");
            }

            if (!Enum.IsDefined(comment.Side))
            {
                errors.Add($"{field}.side", "Choose the original or the modified side.");
            }

            ValidateLineRange(comment.StartLine, comment.EndLine, field, errors);

            if (string.IsNullOrWhiteSpace(comment.Body))
            {
                errors.Add($"{field}.body", "Enter a comment.");
            }
            else if (comment.Body.Length > BodyMaxLength)
            {
                errors.Add($"{field}.body", Invariant($"Keep the comment to at most {BodyMaxLength:N0} characters."));
            }
        }
    }

    private static bool ValidateLineRange(int startLine, int endLine, string field, Errors errors)
    {
        if (startLine < 1)
        {
            errors.Add($"{field}.startLine", "Start at line 1 or later.");
            return false;
        }

        if (endLine < startLine)
        {
            errors.Add($"{field}.endLine", "End at the start line or later.");
            return false;
        }

        return true;
    }

    private static int LineCount(string text)
    {
        var count = 1;
        for (var index = 0; index < text.Length; index++)
        {
            switch (text[index])
            {
                case '\r':
                    count++;
                    if (index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    break;
                case '\n':
                    count++;
                    break;
            }
        }

        return count;
    }

    private static string SideName(DiffSide side) => side == DiffSide.Original ? "original" : "modified";

    private static string Invariant(FormattableString message) => message.ToString(CultureInfo.InvariantCulture);

    [GeneratedRegex("^[a-z][a-z0-9]*(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex KebabCase();

    private sealed class Errors
    {
        private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

        public void Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var messages))
            {
                _errors[field] = messages = [];
            }

            messages.Add(message);
        }

        public Result ToResult() =>
            _errors.Count == 0
                ? Result.Success()
                : new ValidationError(_errors.ToDictionary(error => error.Key, error => error.Value.ToArray(), StringComparer.Ordinal));
    }
}
