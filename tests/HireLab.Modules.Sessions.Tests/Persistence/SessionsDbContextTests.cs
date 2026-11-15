using HireLab.Modules.Sessions.Persistence;

namespace HireLab.Modules.Sessions.Tests.Persistence;

public sealed class SessionsDbContextTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Database_is_migrated_into_the_module_schema()
    {
        // Act and assert
        await Host.ShouldHaveMigratedAsync<SessionsDbContext>();
    }
}
