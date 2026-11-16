using System.Reflection;
using HireLab.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace HireLab.ArchitectureTests;

public sealed class QueryFilterBypassTests
{
    private const string MigrationWorkerAssembly = "HireLab.MigrationService";

    private static readonly string _testAssemblyPath = typeof(QueryFilterBypassTests).Assembly.Location;

    [Fact]
    public void IgnoreQueryFilters_is_called_only_by_the_migration_worker_system_jobs_and_candidate_scopes()
    {
        // Arrange
        var assemblies = ProductionAssemblyPaths();

        // Act
        var violations = ViolationsIn(assemblies);

        // Assert
        assemblies.ShouldContain(path => Path.GetFileNameWithoutExtension(path) == MigrationWorkerAssembly);
        violations.ShouldBeEmpty(
            "IgnoreQueryFilters is allowed only in the migration worker, in system jobs, which are hosted services or marked "
            + "ISystemJob (ADR 0009), and in the candidate's session reads, marked ICandidateScope (ADR 0031)");
    }

    [Fact]
    public void Rule_fails_on_a_planted_violation_and_allows_system_jobs_and_candidate_scopes()
    {
        // Act
        var violations = ViolationsIn([_testAssemblyPath]);

        // Assert
        violations.ShouldNotBeEmpty();
        violations.ShouldAllBe(violation => violation.Type == typeof(QueryFilterBypassSample).FullName);
        var bypasses = QueryFilterBypassScanner.FindIn(_testAssemblyPath);
        bypasses.ShouldContain(bypass => bypass.Type == typeof(QueryFilterBypassSystemJob).FullName);
        bypasses.ShouldContain(bypass => bypass.Type == typeof(QueryFilterBypassMarkedSystemJob).FullName);
        bypasses.ShouldContain(bypass => bypass.Type == typeof(QueryFilterBypassCandidateScope).FullName);
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
            .Where(bypass => bypass.Assembly != MigrationWorkerAssembly && !IsAllowed(bypass))
            .ToList();

    private static List<string> ProductionAssemblyPaths()
    {
        var testAssembly = typeof(QueryFilterBypassTests).Assembly.GetName().Name;

        return Directory.GetFiles(AppContext.BaseDirectory, "HireLab.*.dll")
            .Where(path => Path.GetFileNameWithoutExtension(path) != testAssembly)
            .ToList();
    }

    private static bool IsAllowed(QueryFilterBypass bypass) =>
        Assembly.Load(bypass.Assembly).GetType(bypass.Type) is { } type
        && (type.IsAssignableTo(typeof(IHostedService))
            || type.IsAssignableTo(typeof(ISystemJob))
            || type.IsAssignableTo(typeof(ICandidateScope)));
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

internal sealed class QueryFilterBypassMarkedSystemJob(IQueryable<object> query) : ISystemJob
{
    public Task<List<object>> RunAsync(CancellationToken cancellationToken) => query.IgnoreQueryFilters().ToListAsync(cancellationToken);
}

internal sealed class QueryFilterBypassCandidateScope(IQueryable<object> query) : ICandidateScope
{
    public Task<List<object>> ReadAsync(CancellationToken cancellationToken) => query.IgnoreQueryFilters(["Team"]).ToListAsync(cancellationToken);
}
