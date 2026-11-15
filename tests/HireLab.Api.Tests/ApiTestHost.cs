using HireLab.Api.Tests.Auth;
using HireLab.SharedKernel.Modules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

[assembly: AssemblyFixture(typeof(HireLab.Api.Tests.ApiTestHost))]
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace HireLab.Api.Tests;

public sealed class ApiTestHost : HireLabTestHost
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services => services.AddSingleton<IModule, AuthProbeModule>());
}
