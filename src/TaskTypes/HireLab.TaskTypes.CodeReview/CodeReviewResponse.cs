using System.Text.Json.Serialization;

namespace HireLab.TaskTypes.CodeReview;

/// <summary>
/// The candidate's review: the review comments, the Markdown review summary and an optional verdict. The summary may
/// be empty and the verdict missing while the candidate is still reviewing.
/// </summary>
public sealed record CodeReviewResponse(IReadOnlyList<ReviewComment> Comments, string Summary, ReviewVerdict? Verdict = null);

/// <summary>
/// A Markdown comment on <see cref="StartLine"/>–<see cref="EndLine"/> (1-based, inclusive) of one <see cref="Side"/>
/// of the changed file whose path is <see cref="File"/>. The client generates <see cref="Id"/>, so that whole-document
/// autosave can edit and delete the comment. The anchor isn't checked against the task (ADR 0032).
/// </summary>
public sealed record ReviewComment(Guid Id, string File, DiffSide Side, int StartLine, int EndLine, string Body);

/// <summary>The candidate's decision on the pull request. Serialised by name: <c>Approve</c>, <c>RequestChanges</c>, <c>Comment</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReviewVerdict>))]
public enum ReviewVerdict
{
    Approve,
    RequestChanges,
    Comment,
}
