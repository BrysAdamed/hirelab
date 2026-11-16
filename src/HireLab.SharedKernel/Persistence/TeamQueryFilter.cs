using System.Linq.Expressions;
using System.Reflection;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// The global query filter the module conventions add to every <see cref="ITeamOwned"/> entity (ADR 0009): a row is
/// visible only when its <c>TeamId</c> is the <see cref="ICurrentUser.TeamId"/> of the context's scope. The admin,
/// candidates, anonymous requests and code outside a request have no team, so they see no rows. Only the migration
/// worker and system jobs may bypass it with <c>IgnoreQueryFilters</c>.
/// </summary>
public static class TeamQueryFilter
{
    /// <summary>The key of the filter, as EF Core named query filters take it.</summary>
    public const string Name = "Team";

    private static readonly MethodInfo _currentTeamOf =
        typeof(TeamQueryFilter).GetMethod(nameof(CurrentTeamOf), BindingFlags.NonPublic | BindingFlags.Static)!;

    internal static void AddTo(ModelBuilder modelBuilder, DbContext context)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(IsTeamOwned).ToList())
        {
            if (entityType.IsOwned() || (entityType.BaseType is { } baseType && !IsTeamOwned(baseType)))
            {
                throw new InvalidOperationException(
                    $"{entityType.ClrType.Name} is team-owned but is an owned type or derives from an entity type that isn't. Mark the root entity type with {nameof(ITeamOwned)} instead.");
            }

            if (entityType.BaseType is not null)
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Name, FilterFor(entityType.ClrType, context));
        }
    }

    private static bool IsTeamOwned(IMutableEntityType entityType) =>
        entityType.ClrType.IsAssignableTo(typeof(ITeamOwned));

    private static LambdaExpression FilterFor(Type entityClrType, DbContext context)
    {
        var entity = Expression.Parameter(entityClrType, "entity");
        var entityTeam = Expression.Convert(Expression.Property(entity, nameof(ITeamOwned.TeamId)), typeof(TeamId?));
        var currentTeam = Expression.Call(_currentTeamOf, Expression.Constant(context));

        return Expression.Lambda(Expression.Equal(entityTeam, currentTeam), entity);
    }

    private static TeamId? CurrentTeamOf(DbContext context) =>
        context.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?
            .ApplicationServiceProvider?
            .GetService<ICurrentUser>()?
            .TeamId;
}
