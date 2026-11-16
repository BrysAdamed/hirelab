using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace HireLab.ArchitectureTests;

public sealed class QueryFilterBypassTests
{
    private const string MigrationWorkerAssembly = "HireLab.MigrationService";

    private static readonly string _testAssemblyPath = typeof(QueryFilterBypassTests).Assembly.Location;

    [Fact]
    public void IgnoreQueryFilters_is_called_only_by_the_migration_worker_and_system_jobs()
    {
        // Arrange
        var assemblies = ProductionAssemblyPaths();

        // Act
        var violations = ViolationsIn(assemblies);

        // Assert
        assemblies.ShouldContain(path => Path.GetFileNameWithoutExtension(path) == MigrationWorkerAssembly);
        violations.ShouldBeEmpty("IgnoreQueryFilters is allowed only in the migration worker and system jobs, which are hosted services (ADR 0009)");
    }

    [Fact]
    public void Rule_fails_on_a_planted_violation_and_allows_a_system_job()
    {
        // Act
        var violations = ViolationsIn([_testAssemblyPath]);

        // Assert
        violations.ShouldNotBeEmpty();
        violations.ShouldAllBe(violation => violation.Type == typeof(QueryFilterBypassSample).FullName);
        QueryFilterBypassScanner.FindIn(_testAssemblyPath).ShouldContain(bypass => bypass.Type == typeof(QueryFilterBypassSystemJob).FullName);
    }

    [Fact]
    public void Scanner_finds_IgnoreQueryFilters_in_methods_lambdas_and_async_methods()
    {
        // Act
        var bypasses = QueryFilterBypassScanner.FindIn(_testAssemblyPath);

        // Assert
        bypasses.Where(bypass => bypass.Type == typeof(QueryFilterBypassSample).FullName).Count().ShouldBe(4);
    }

    private static List<QueryFilterBypass> ViolationsIn(IEnumerable<string> assemblyPaths) =>
        assemblyPaths
            .SelectMany(QueryFilterBypassScanner.FindIn)
            .Where(bypass => bypass.Assembly != MigrationWorkerAssembly && !IsSystemJob(bypass))
            .ToList();

    private static List<string> ProductionAssemblyPaths()
    {
        var testAssembly = typeof(QueryFilterBypassTests).Assembly.GetName().Name;

        return Directory.GetFiles(AppContext.BaseDirectory, "HireLab.*.dll")
            .Where(path => Path.GetFileNameWithoutExtension(path) != testAssembly)
            .ToList();
    }

    private static bool IsSystemJob(QueryFilterBypass bypass) =>
        Assembly.Load(bypass.Assembly).GetType(bypass.Type)?.IsAssignableTo(typeof(IHostedService)) == true;
}

internal static class QueryFilterBypassSample
{
    public static IQueryable<object> Bypass(IQueryable<object> query) => query.IgnoreQueryFilters();

    public static IQueryable<object> BypassNamedFilter(IQueryable<object> query) => query.IgnoreQueryFilters(["Team"]);

    public static Func<IQueryable<object>, IQueryable<object>> BypassInLambda() => query => query.IgnoreQueryFilters();

    public static async Task<List<object>> BypassAsync(IQueryable<object> query) => await query.IgnoreQueryFilters().ToListAsync();
}

internal sealed class QueryFilterBypassSystemJob(IQueryable<object> query) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => query.IgnoreQueryFilters().ToListAsync(stoppingToken);
}
