using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace HireLab.Testing;

/// <summary>Assertions on a module's database, for the module test projects.</summary>
public static class ModuleDatabaseAssertions
{
    /// <summary>
    /// Asserts that all of <typeparamref name="TContext"/>'s migrations are applied and that its migrations history
    /// lives in the module schema, which proves the schema exists.
    /// </summary>
    public static async Task ShouldHaveMigratedAsync<TContext>(this HireLabTestHost host)
        where TContext : DbContext, IModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(host);
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var applied = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        applied.ShouldBe(dbContext.Database.GetMigrations());
        applied.ShouldNotBeEmpty();

        var historyTables = await dbContext.Database
            .SqlQuery<int>($"""
                SELECT count(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = {TContext.Schema} AND table_name = '__EFMigrationsHistory'
                """)
            .SingleAsync(cancellationToken);
        historyTables.ShouldBe(1, $"The migrations history of {typeof(TContext).Name} should be in the '{TContext.Schema}' schema.");
    }
}
