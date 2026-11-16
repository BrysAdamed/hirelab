namespace HireLab.TaskTypes.Abstractions;

/// <summary>The task types registered in the host, by key.</summary>
public interface ITaskTypeRegistry
{
    /// <summary>Every registered task type, ordered by key.</summary>
    IReadOnlyList<ITaskType> All { get; }

    /// <summary>The task type with <paramref name="key"/>, or <c>null</c> when no such type is registered.</summary>
    ITaskType? Find(string key);
}
