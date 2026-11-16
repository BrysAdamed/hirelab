using System.Text.Json;
using HireLab.Api.Tests.Auth;
using HireLab.Testing.TaskTypes;

namespace HireLab.Api.Tests.OpenApi;

public sealed class CandidateOpenApiContractTests(ApiTestHost host) : IntegrationTest(host)
{
    private static readonly string[] _sharedSchemas = ["ProblemDetails", "HttpValidationProblemDetails"];

    private static readonly string[] _staffOnlyFieldNames =
    [
        "referenceanswer",
        "interviewernote",
        "rubric",
        "staffonly",
        "staffcontent",
    ];

    [Fact]
    public async Task Candidate_document_contains_only_candidate_paths()
    {
        // Act
        var paths = PathsOf(await GetDocumentAsync("candidate"));

        // Assert
        paths.ShouldContain(AuthProbeModule.CandidatePath);
        paths.ShouldContain(TestEchoTaskType.EchoPath);
        paths.ShouldAllBe(path => path.StartsWith(GroupEndpoint.CandidateGroup + "/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Staff_document_contains_only_staff_paths()
    {
        // Act
        var paths = PathsOf(await GetDocumentAsync("staff"));

        // Assert
        paths.ShouldContain(AuthProbeModule.StaffPath);
        paths.ShouldAllBe(path => path.StartsWith(GroupEndpoint.StaffGroup + "/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Candidate_document_shares_no_schemas_with_the_staff_document()
    {
        // Arrange
        var staffSchemas = SchemasOf(await GetDocumentAsync("staff")).Select(schema => schema.Name).ToHashSet();

        // Act
        var candidateSchemas = SchemasOf(await GetDocumentAsync("candidate")).Select(schema => schema.Name);

        // Assert
        candidateSchemas.Where(name => staffSchemas.Contains(name) && !_sharedSchemas.Contains(name))
            .ShouldBeEmpty("Candidate endpoints must use their own DTOs, so that staff-only fields can't reach the candidate");
    }

    [Fact]
    public async Task Candidate_document_contains_no_staff_only_fields()
    {
        // Act
        var fields = StaffOnlyFieldsOf(await GetDocumentAsync("candidate"));

        // Assert
        fields.ShouldBeEmpty("The candidate API must never return reference answers, interviewer notes, rubrics or staff content (ADR 0013, ADR 0028)");
    }

    [Fact]
    public void Staff_only_fields_are_found_in_nested_schemas()
    {
        // Arrange
        using var document = JsonDocument.Parse("""
            {
              "components": {
                "schemas": {
                  "ExampleTask": {
                    "properties": {
                      "title": { "type": "string" },
                      "content": {
                        "type": "object",
                        "properties": { "reference_answer": { "type": "string" } }
                      },
                      "interviewerNotes": { "type": "array", "items": { "type": "string" } },
                      "staffContent": { "type": "object" }
                    }
                  }
                }
              }
            }
            """);

        // Act
        var fields = StaffOnlyFieldsOf(document.RootElement);

        // Assert
        fields.ShouldBe(["reference_answer", "interviewerNotes", "staffContent"], ignoreOrder: true);
    }

    private async Task<JsonElement> GetDocumentAsync(string documentName)
    {
        using var client = Host.CreateClient();
        using var response = await client.GetAsync(new Uri($"/openapi/{documentName}.json", UriKind.Relative), CancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        return document.RootElement.Clone();
    }

    private static List<string> StaffOnlyFieldsOf(JsonElement document) =>
        PropertyNamesOf(SchemasOf(document).Select(schema => schema.Value))
            .Where(name => _staffOnlyFieldNames.Any(staffOnly => name.Replace("_", string.Empty, StringComparison.Ordinal)
                .Contains(staffOnly, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    private static List<string> PathsOf(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToList();

    private static List<JsonProperty> SchemasOf(JsonElement document) =>
        document.TryGetProperty("components", out var components) && components.TryGetProperty("schemas", out var schemas)
            ? schemas.EnumerateObject().ToList()
            : [];

    private static IEnumerable<string> PropertyNamesOf(IEnumerable<JsonElement> elements) =>
        elements.SelectMany(element => element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().SelectMany(property =>
                property.Name == "properties" && property.Value.ValueKind == JsonValueKind.Object
                    ? property.Value.EnumerateObject().Select(field => field.Name)
                        .Concat(PropertyNamesOf(property.Value.EnumerateObject().Select(field => field.Value)))
                    : PropertyNamesOf([property.Value])),
            JsonValueKind.Array => PropertyNamesOf(element.EnumerateArray()),
            _ => [],
        });
}
