using HireLab.Modules.TaskBank.Persistence;

namespace HireLab.Modules.TaskBank.Tests.Persistence;

public sealed class TaskBankDbContextTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Database_is_migrated_into_the_module_schema()
    {
        // Act and assert
        await Host.ShouldHaveMigratedAsync<TaskBankDbContext>();
    }
}
