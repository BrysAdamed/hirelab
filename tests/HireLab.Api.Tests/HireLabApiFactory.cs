using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HireLab.Api.Tests;

public sealed class HireLabApiFactory : WebApplicationFactory<Program>
{
    private static readonly string _testWebRoot = Path.Combine(AppContext.BaseDirectory, "TestWebRoot");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.WebRootKey, _testWebRoot);
        builder.UseSetting("ConnectionStrings:hirelab", "Host=localhost;Database=hirelab-tests");
        builder.UseSetting("ConnectionStrings:blobs", "UseDevelopmentStorage=true");
    }
}
