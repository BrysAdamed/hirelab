using System.Text.Json.Serialization;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>
/// How the task bank labels a task: a team task seen by its team only (<see cref="Team"/>), a team task every team sees
/// (<see cref="Global"/>), or a <see cref="System"/> task.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BankVisibility>))]
internal enum BankVisibility
{
    Team = 1,
    Global = 2,
    System = 3,
}

internal static class BankVisibilities
{
    public static BankVisibility Of(bool isSystem, TaskVisibility visibility) =>
        isSystem ? BankVisibility.System
        : visibility == TaskVisibility.Global ? BankVisibility.Global
        : BankVisibility.Team;
}
