using System.Text.Json;
using HireLab.SharedKernel.Results;
using HireLab.TaskTypes.Abstractions;

namespace HireLab.TaskTypes.CodeReview.Tests;

internal static class CodeReviewBuilders
{
    public const string ModifiedPath = "src/FakeShop/FakeOrderHandler.cs";

    public const string AddedPath = "src/FakeShop/FakeDiscount.cs";

    public const string DeletedPath = "src/FakeShop/LegacyFakeCoupon.cs";

    public static ChangedFile ModifiedFile { get; } = new(
        ModifiedPath,
        "csharp",
        "public class FakeOrderHandler\n{\n    public void Handle() { }\n}\n",
        "public class FakeOrderHandler\n{\n    public void Handle(FakeOrder order)\n    {\n        order.Save();\n    }\n}\n");

    public static ChangedFile AddedFile { get; } = new(AddedPath, "csharp", Modified: "public record FakeDiscount(decimal Percent);");

    public static ChangedFile DeletedFile { get; } = new(DeletedPath, "csharp", Original: "public class LegacyFakeCoupon { }");

    public static CodeReviewCandidateContent Candidate { get; } = new(
        "Fake: save orders in the handler",
        "## Made-up change\n\nSaves a **fake** order.",
        [ModifiedFile, AddedFile, DeletedFile]);

    public static ExpectedFinding Finding { get; } = new(
        "missing-null-check",
        ModifiedPath,
        DiffSide.Modified,
        StartLine: 3,
        EndLine: 5,
        "Fake missing null check",
        "The made-up `order` may be null.",
        FindingSeverity.Major);

    public static CodeReviewStaffContent Staff { get; } = new([Finding]);

    public static ReviewComment Comment { get; } = new(
        Guid.Parse("0196f0c4-0000-7000-8000-000000000001"),
        ModifiedPath,
        DiffSide.Modified,
        StartLine: 3,
        EndLine: 3,
        "What if the fake `order` is null?");

    public static CodeReviewResponse Response { get; } = new([Comment], "Looks fake but fine.", ReviewVerdict.RequestChanges);

    public static ReviewComment CommentNumbered(int number) =>
        Comment with { Id = Guid.Parse($"0196f0c4-0000-7000-8000-{number:D12}") };

    public static JsonElement ToJson<T>(T value) => JsonSerializer.SerializeToElement(value, TaskTypeJson.Options);

    public static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public static IReadOnlyDictionary<string, string[]> ErrorsOf(Result result)
    {
        result.IsFailure.ShouldBeTrue();
        return result.Error.ShouldBeOfType<ValidationError>().Errors;
    }
}
