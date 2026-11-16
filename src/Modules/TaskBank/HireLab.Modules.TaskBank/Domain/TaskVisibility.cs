using System.Text.Json.Serialization;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>Who sees a task (ADR 0015): only its owning team, or every team. System tasks are always <see cref="Global"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TaskVisibility>))]
internal enum TaskVisibility
{
    Team = 1,
    Global = 2,
}
