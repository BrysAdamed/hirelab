using HireLab.Modules.Positions.Persistence;

namespace HireLab.Modules.Positions.Tests.Persistence;

public sealed class PositionsDbContextTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Database_is_migrated_into_the_module_schema()
    {
        // Act and assert
        await Host.ShouldHaveMigratedAsync<PositionsDbContext>();
    }
}
