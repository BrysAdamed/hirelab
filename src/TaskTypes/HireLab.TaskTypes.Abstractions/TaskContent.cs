namespace HireLab.TaskTypes.Abstractions;

/// <summary>A task version's content: the part the candidate sees and the staff-only part.</summary>
public sealed record TaskContent<TCandidateContent, TStaffContent>(TCandidateContent Candidate, TStaffContent Staff)
    where TCandidateContent : class
    where TStaffContent : class;
