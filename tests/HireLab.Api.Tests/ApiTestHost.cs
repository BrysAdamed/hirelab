using HireLab.Api.Tests.Auth;
using HireLab.SharedKernel.Modules;
using HireLab.SharedKernel.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(HireLab.Api.Tests.ApiTestHost))]
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace HireLab.Api.Tests;

public sealed class ApiTestHost : HireLabTestHost
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IModule, AuthProbeModule>();
            services.AddSingleton<IModule, TeamScopeProbeModule>();
            services.AddModuleDbContext<TeamNotesDbContext>();
        });

    protected override async Task CreateTestTablesAsync()
    {
        var options = new DbContextOptionsBuilder<TeamNotesDbContext>();
        options.UseModuleConventions<TeamNotesDbContext>(DatabaseConnectionString);

        await using var dbContext = new TeamNotesDbContext(options.Options);
        await dbContext.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
    }
}
