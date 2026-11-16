using System.Text.Json.Serialization;

namespace HireLab.Modules.TaskBank.Domain;

/// <summary>A task version is a <see cref="Draft"/> until it is published; a published version is immutable.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TaskVersionState>))]
internal enum TaskVersionState
{
    Draft = 1,
    Published = 2,
}
