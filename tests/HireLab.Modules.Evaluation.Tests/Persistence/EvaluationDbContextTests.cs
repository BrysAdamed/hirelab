using HireLab.Modules.Evaluation.Persistence;

namespace HireLab.Modules.Evaluation.Tests.Persistence;

public sealed class EvaluationDbContextTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task Database_is_migrated_into_the_module_schema()
    {
        // Act and assert
        await Host.ShouldHaveMigratedAsync<EvaluationDbContext>();
    }
}
