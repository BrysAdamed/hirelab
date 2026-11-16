using System.Text.Json.Serialization;

namespace HireLab.Modules.Sessions.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<InterviewSessionState>))]
internal enum InterviewSessionState
{
    Planned = 1,
    Live = 2,
    Finished = 3,
}
