using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Testing;

/// <summary>
/// Reproduces a request that loses a race: <see cref="BeforeNextSave{TEntity}"/> runs a change, as another request
/// would, just before the API's next save that touches a given entity type in a given state.
/// </summary>
public sealed class ConcurrentChange : SaveChangesInterceptor
{
    private static readonly MethodInfo _addTo =
        typeof(ConcurrentChange).GetMethod(nameof(AddTo), BindingFlags.NonPublic | BindingFlags.Static)!;

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

    internal static void AddToEveryDbContext(IServiceCollection services, ConcurrentChange concurrentChange)
    {
        var contextTypes = services
            .Select(descriptor => descriptor.ServiceType)
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
            .Select(type => type.GenericTypeArguments[0])
            .Distinct()
            .ToList();

        foreach (var contextType in contextTypes)
        {
            _addTo.MakeGenericMethod(contextType).Invoke(null, [services, concurrentChange]);
        }
    }

    private static void AddTo<TContext>(IServiceCollection services, ConcurrentChange concurrentChange)
        where TContext : DbContext =>
        services.ConfigureDbContext<TContext>(options => options.AddInterceptors(concurrentChange));

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
