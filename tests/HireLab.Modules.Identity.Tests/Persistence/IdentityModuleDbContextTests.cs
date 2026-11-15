using HireLab.Modules.Identity.Persistence;

namespace HireLab.Modules.Identity.Tests.Persistence;

public sealed class IdentityModuleDbContextTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Database_is_migrated_into_the_module_schema()
    {
        // Act and assert
        await Host.ShouldHaveMigratedAsync<IdentityModuleDbContext>();
    }
}
