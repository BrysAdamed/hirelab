using System.Text.Json.Serialization;

namespace HireLab.Modules.TaskBank.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<TaskLevel>))]
internal enum TaskLevel
{
    Junior = 1,
    Mid = 2,
    Senior = 3,
}
