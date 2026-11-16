using System.Text.Json;
using HireLab.Modules.Sessions.Contracts;
using HireLab.SharedKernel.Results;

namespace HireLab.Modules.Sessions.Domain;

/// <summary>
/// Everything a candidate produced for one <see cref="SessionTask"/> (ADR 0031): the task type's response model as
/// <c>jsonb</c>, stored whole on every autosave. <see cref="Revision"/> goes up with every save, and a save must name the
/// revision it was based on, so an older tab can't overwrite a newer response.
/// </summary>
internal sealed class Response
{
    public const int MaxBytes = 1024 * 1024;

    private Response()
    {
    }

    public SessionTaskId SessionTaskId { get; private set; }

    public InterviewSessionId SessionId { get; private set; }

    public JsonElement Content { get; private set; }

    public int ResponseSchemaVersion { get; private set; }

    /// <summary>The number of saves so far; 0 means nothing has been saved.</summary>
    public int Revision { get; private set; }

    public static Response Start(InterviewSessionId sessionId, SessionTaskId sessionTaskId) =>
        new() { SessionId = sessionId, SessionTaskId = sessionTaskId };

    /// <summary>Replaces the content when <paramref name="basedOnRevision"/> is still the current revision.</summary>
    public Result Save(JsonElement content, int responseSchemaVersion, int basedOnRevision)
    {
        if (basedOnRevision != Revision)
        {
            return ResponseErrors.StaleRevision;
        }

        Content = content.Clone();
        ResponseSchemaVersion = responseSchemaVersion;
        Revision++;
        return Result.Success();
    }
}

/// <summary>A saved response as it is read back: its content and revision.</summary>
internal sealed record StoredResponse(JsonElement Content, int Revision);
