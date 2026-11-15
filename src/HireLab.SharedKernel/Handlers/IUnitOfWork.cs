namespace HireLab.SharedKernel.Handlers;

/// <summary>
/// The module's persistence boundary. A module's <c>DbContext</c> implements it without extra code,
/// because <c>DbContext.SaveChangesAsync(CancellationToken)</c> already matches.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
