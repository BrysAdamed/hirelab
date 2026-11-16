using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.TaskTypes.Abstractions;

/// <summary>The typed view of a task type, which its own code implements.</summary>
public interface ITaskType<TCandidateContent, TStaffContent, TResponse> : ITaskType
    where TCandidateContent : class
    where TStaffContent : class
    where TResponse : class
{
    /// <summary>Validates the type's own rules for a task version's content. A failure is a <see cref="ValidationError"/>.</summary>
    Result ValidateContent(TaskContent<TCandidateContent, TStaffContent> content);

    /// <summary>Validates the type's own rules for a candidate's response. A failure is a <see cref="ValidationError"/>.</summary>
    Result ValidateResponse(TResponse response);

    /// <summary>Suggests values for the criteria of <paramref name="rubric"/> that the type can judge from the response.</summary>
    IReadOnlyList<CriterionSuggestion> SuggestScores(TaskContent<TCandidateContent, TStaffContent> content, Rubric rubric, TResponse response);
}
