using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HireLab.Testing.TaskTypes;

/// <summary>
/// Builds the files of a valid <see cref="TestEchoTaskType"/> seed package (ADR 0030) with obviously fake values: a
/// <c>task.json</c> whose reference answer is inlined from <c>reference-answer.md</c>. Paths are below <c>src/Tasks</c>.
/// </summary>
public sealed class TestEchoSeedPackage(string slug, int number = 1)
{
    public const string ReferenceAnswerFile = "reference-answer.md";
    public const string ReferenceAnswer = "A made-up reference answer.\nIt has two lines.\n";

    private static readonly JsonSerializerOptions _indented = new() { WriteIndented = true };

    private readonly JsonObject _task = new()
    {
        ["taskType"] = TestEchoTaskType.TaskTypeKey,
        ["title"] = $"Fake Echo Task {slug}",
        ["summary"] = "A made-up task for tests.",
        ["level"] = "Mid",
        ["estimatedMinutes"] = 15,
        ["tags"] = new JsonArray("fake", "testing"),
        ["referenceAnswer"] = FileReference(ReferenceAnswerFile),
        ["rubric"] = new JsonObject
        {
            ["criteria"] = new JsonArray(
                new JsonObject
                {
                    ["type"] = "scale",
                    ["id"] = "fake-quality",
                    ["title"] = "Fake quality",
                    ["description"] = "How convincingly fake it is.",
                    ["weight"] = 2,
                },
                new JsonObject
                {
                    ["type"] = "check",
                    ["id"] = "fake-echo",
                    ["title"] = "Fake echo",
                    ["description"] = "Did the candidate echo the phrase?",
                    ["expected"] = "yes",
                    ["critical"] = true,
                }),
        },
        ["candidateContent"] = new JsonObject { ["phrase"] = "Hello, fake world" },
        ["staffContent"] = new JsonObject { ["matchCriterionId"] = "fake-echo" },
    };

    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal)
    {
        [ReferenceAnswerFile] = ReferenceAnswer,
    };

    public string Slug => slug;

    public int Number => number;

    /// <summary>The package folder below <c>src/Tasks</c>, for example <c>fake-task/v1</c>.</summary>
    public string Folder => $"{slug}/v{number.ToString(CultureInfo.InvariantCulture)}";

    public static JsonObject FileReference(string path) => new() { ["$file"] = path };

    public TestEchoSeedPackage WithPhrase(string phrase)
    {
        _task["candidateContent"] = new JsonObject { ["phrase"] = phrase };
        return this;
    }

    /// <summary>Sets a top-level <c>task.json</c> field, for example <c>title</c>.</summary>
    public TestEchoSeedPackage With(string field, JsonNode? value)
    {
        _task[field] = value;
        return this;
    }

    public TestEchoSeedPackage Without(string field)
    {
        _task.Remove(field);
        return this;
    }

    /// <summary>Adds or replaces a content file, with a path relative to the package folder.</summary>
    public TestEchoSeedPackage WithFile(string path, string text)
    {
        _files[path] = text;
        return this;
    }

    public TestEchoSeedPackage WithoutFile(string path)
    {
        _files.Remove(path);
        return this;
    }

    /// <summary>The package's files by path below <c>src/Tasks</c>, with <c>task.json</c> first.</summary>
    public IReadOnlyList<KeyValuePair<string, byte[]>> Files() =>
    [
        new($"{Folder}/task.json", JsonSerializer.SerializeToUtf8Bytes(_task, _indented)),
        .. _files.Select(file => new KeyValuePair<string, byte[]>($"{Folder}/{file.Key}", Encoding.UTF8.GetBytes(file.Value))),
    ];
}
