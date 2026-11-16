using System.Text.Json;
using System.Text.Json.Nodes;
using HireLab.Api.Realtime;

namespace HireLab.Api.Tests.Realtime;

/// <summary>
/// Keeps the committed hub contracts in <c>src/web/projects/api-client/hubs</c>, which the web workspace generates its
/// hub types from, in step with the typed hubs. Run the tests with <c>HIRELAB_UPDATE_HUB_CONTRACTS=1</c> to rewrite
/// them, then <c>npm run api:update</c> in <c>src/web</c>.
/// </summary>
public sealed class HubContractTests
{
    private const string UpdateVariable = "HIRELAB_UPDATE_HUB_CONTRACTS";

    private static readonly JsonSerializerOptions _writeOptions = new() { WriteIndented = true };

    public static TheoryData<string> Hubs => ["staff", "candidate"];

    [Theory]
    [MemberData(nameof(Hubs))]
    public void The_committed_hub_contract_matches_the_hub(string hub)
    {
        // Arrange
        var path = Path.Combine(RepositoryRoot(), "src", "web", "projects", "api-client", "hubs", $"{hub}.json");
        var actual = hub == "staff"
            ? HubContract.Describe(typeof(StaffHub), typeof(IStaffClient), StaffHub.Path)
            : HubContract.Describe(typeof(CandidateHub), typeof(ICandidateClient), CandidateHub.Path);
        var actualJson = actual.ToJsonString(_writeOptions).ReplaceLineEndings("\n") + "\n";

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, actualJson);
        }

        // Act
        var committed = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) : null;

        // Assert
        JsonNode.DeepEquals(committed, actual).ShouldBeTrue(
            $"{path} doesn't match the hub. Run the backend tests with {UpdateVariable}=1 and then `npm run api:update` in src/web, or commit this:\n{actualJson}");
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HireLab.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"HireLab.slnx was not found above {AppContext.BaseDirectory}.");
    }
}
