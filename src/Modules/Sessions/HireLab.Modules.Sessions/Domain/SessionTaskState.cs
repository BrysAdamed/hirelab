using System.Text.Json.Serialization;

namespace HireLab.Modules.Sessions.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<SessionTaskState>))]
internal enum SessionTaskState
{
    Pending = 1,
    Current = 2,
    Done = 3,
}
