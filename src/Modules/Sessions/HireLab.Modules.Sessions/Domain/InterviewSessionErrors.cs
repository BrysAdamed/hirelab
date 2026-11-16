using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.Domain;

internal static class InterviewSessionErrors
{
    public static readonly Error NotFound = Error.NotFound("InterviewSession.NotFound", "The interview session was not found.");

    public static readonly Error CandidateNotFound = Error.NotFound("Candidate.NotFound", "The candidate was not found.");

    public static readonly Error CandidateAnonymised = Error.Conflict(
        "InterviewSession.CandidateAnonymised",
        "This candidate has been anonymised, so they can't be interviewed any more.");

    public static readonly Error PositionClosed = Error.Conflict(
        "InterviewSession.PositionClosed",
        "This candidate's job position is closed, so they can't be interviewed any more.");

    public static readonly Error NotPlanned = Error.Conflict(
        "InterviewSession.NotPlanned",
        "This session has already started. Only a planned session can be changed this way.");

    public static readonly Error TaskNotFound = Error.NotFound("Task.NotFound", "The task was not found in the task bank.");

    public static readonly Error SessionTaskNotFound = Error.NotFound(
        "InterviewSession.SessionTaskNotFound",
        "The task is not in this session's queue.");

    public static readonly Error TaskAlreadyQueued = Error.Conflict(
        "InterviewSession.TaskAlreadyQueued",
        "This task is already in the session's queue.");

    public static readonly Error QueueFull = Error.Conflict(
        "InterviewSession.QueueFull",
        $"A session's queue holds at most {InterviewSession.MaxTasks} tasks.");

    public static readonly Error OrderMismatch = Error.Conflict(
        "InterviewSession.OrderMismatch",
        "The new order doesn't list exactly the tasks in the queue. Reload and try again.");
}
