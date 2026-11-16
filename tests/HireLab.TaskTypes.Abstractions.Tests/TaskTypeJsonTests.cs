using System.Text.Json;
using HireLab.TaskTypes.Abstractions.Rubrics;

namespace HireLab.TaskTypes.Abstractions.Tests;

public sealed class TaskTypeJsonTests
{
    [Fact]
    public void A_rubric_reads_back_when_jsonb_put_the_type_discriminator_after_other_properties()
    {
        // Arrange
        const string json = """
            {
              "criteria": [
                { "id": "fake-quality", "title": "Fake quality", "description": "Fake.", "weight": 2, "type": "scale" },
                { "id": "fake-echo", "title": "Fake echo", "description": "Fake.", "expected": "yes", "type": "check" }
              ]
            }
            """;

        // Act
        var rubric = JsonSerializer.Deserialize<Rubric>(json, TaskTypeJson.Options);

        // Assert
        rubric.ShouldNotBeNull().Criteria.Select(criterion => criterion.GetType()).ShouldBe([typeof(ScaleCriterion), typeof(CheckCriterion)]);
    }
}
