using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Contracts;

/// <summary>
/// Looks up staff users for other modules, which store only the <see cref="UserId"/> of whoever did something and show
/// that user's current display name. Registered as a scoped service by the Identity module.
/// </summary>
public interface IStaffDirectory
{
    /// <summary>
    /// The current display names of the users in <paramref name="userIds"/>, keyed by id, whatever their team or active
    /// state. Ids of users that don't exist are missing from the result.
    /// </summary>
    Task<IReadOnlyDictionary<UserId, string>> FindDisplayNamesAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken cancellationToken = default);
}
