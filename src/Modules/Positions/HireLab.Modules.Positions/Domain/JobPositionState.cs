using System.Text.Json.Serialization;

namespace HireLab.Modules.Positions.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<JobPositionState>))]
internal enum JobPositionState
{
    Open = 1,
    Closed = 2,
}
