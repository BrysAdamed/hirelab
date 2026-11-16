using Microsoft.EntityFrameworkCore;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// Stale-edit detection on top of the <c>xmin</c> row version (ADR 0004). A client reads an aggregate with its
/// <c>version</c> and sends it back with the change; the handler loads the aggregate, calls
/// <see cref="ExpectVersion{TEntity}"/> and changes it. When someone else saved the row in between, the save fails and the
/// unit of work returns <see cref="Results.Error.ChangedMeanwhile"/> as a <c>409</c>.
/// </summary>
public static class OptimisticConcurrency
{
    /// <summary>
    /// Makes the next save of <paramref name="entity"/> succeed only while its row version is still
    /// <paramref name="expectedVersion"/>. The entity must be tracked and have exactly one row-version property.
    /// </summary>
    public static void ExpectVersion<TEntity>(this DbContext dbContext, TEntity entity, uint expectedVersion)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entity);

        var entry = dbContext.Entry(entity);
        var rowVersion = entry.Metadata.GetProperties().SingleOrDefault(property => property.IsConcurrencyToken && property.ClrType == typeof(uint))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no uint row-version property. Map one with IsRowVersion().");

        entry.Property(rowVersion).OriginalValue = expectedVersion;
    }
}
