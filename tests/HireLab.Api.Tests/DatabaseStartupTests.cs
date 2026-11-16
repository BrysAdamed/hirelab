using HireLab.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace HireLab.Api.Tests;

public sealed class DatabaseStartupTests(HireLabApiFactory factory) : IClassFixture<HireLabApiFactory>
{
    [Fact]
    public void The_data_source_never_attempts_gss_encryption()
    {
        // Act
        var dataSource = factory.Services.GetRequiredService<NpgsqlDataSource>();

        // Assert
        new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).GssEncryptionMode.ShouldBe(GssEncryptionMode.Disable);
    }

    [Fact]
    public void Every_module_DbContext_model_is_built_while_the_host_starts()
    {
        // Act
        var warmup = factory.Services.GetServices<IHostedService>().OfType<DbContextModelWarmup>().ShouldHaveSingleItem();

        // Assert
        warmup.ContextTypes.Select(type => type.Name).ShouldBe(
            ["IdentityModuleDbContext", "PositionsDbContext", "TaskBankDbContext", "SessionsDbContext", "EvaluationDbContext"],
            ignoreOrder: true);
    }
}
