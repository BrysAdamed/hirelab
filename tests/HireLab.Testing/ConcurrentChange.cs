using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HireLab.Testing;

/// <summary>
/// Reproduces a request that loses a race: <see cref="BeforeNextSave{TEntity}"/> runs a change, as another request
/// would, just before the API's next save that touches a given entity type in a given state.
/// </summary>
public sealed class ConcurrentChange : SaveChangesInterceptor
{
    private Trigger? _trigger;

    /// <summary>
    /// Runs <paramref name="change"/> once, before the next save with a <typeparamref name="TEntity"/> in
    /// <paramref name="state"/>. The change should use its own service scope; it commits before that save goes on.
    /// </summary>
    public void BeforeNextSave<TEntity>(EntityState state, Func<Task> change)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(change);
        _trigger = new Trigger(context => context.ChangeTracker.Entries<TEntity>().Any(entry => entry.State == state), change);
    }

    /// <summary>Forgets a change that hasn't run.</summary>
    public void Reset() => _trigger = null;

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (_trigger is { } trigger && eventData.Context is { } context && trigger.Matches(context)
            && Interlocked.CompareExchange(ref _trigger, null, trigger) == trigger)
        {
            await trigger.Change();
        }

        return result;
    }

    private sealed record Trigger(Func<DbContext, bool> Matches, Func<Task> Change);
}
