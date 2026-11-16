using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// The <c>created_at</c> and <c>updated_at</c> columns every module table has (ADR 0004). They are shadow properties,
/// added to every entity type by the module conventions and set from the injected <see cref="TimeProvider"/> on save.
/// </summary>
public static class AuditTimestamps
{
    /// <summary>The shadow property set when a row is inserted.</summary>
    public const string CreatedAt = nameof(CreatedAt);

    /// <summary>The shadow property set when a row is inserted or updated.</summary>
    public const string UpdatedAt = nameof(UpdatedAt);

    internal static void AddTo(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(entityType => !entityType.IsOwned() && entityType.FindPrimaryKey() is not null))
        {
            var entity = modelBuilder.Entity(entityType.Name);
            entity.Property<DateTimeOffset>(CreatedAt);
            entity.Property<DateTimeOffset>(UpdatedAt);
        }
    }

    internal sealed class Interceptor(TimeProvider timeProvider) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Stamp(eventData.Context);
            return ValueTask.FromResult(result);
        }

        private void Stamp(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            var now = timeProvider.GetUtcNow();
            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Metadata.FindProperty(UpdatedAt) is null)
                {
                    continue;
                }

                if (entry.State == EntityState.Added)
                {
                    entry.Property(CreatedAt).CurrentValue = now;
                    entry.Property(UpdatedAt).CurrentValue = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(UpdatedAt).CurrentValue = now;
                }
            }
        }
    }
}
