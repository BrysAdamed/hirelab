using System.Reflection;

namespace HireLab.Api.OpenApi;

/// <summary>
/// Tells whether the host runs inside the build's OpenAPI document generator (<c>npm run api:update</c> and
/// <c>api:check</c>), which starts the API without a database or any other infrastructure. Startup work that needs
/// them is skipped then.
/// </summary>
internal static class BuildTimeDocumentGeneration
{
    private const string GeneratorAssembly = "GetDocument.Insider";

    public static bool IsRunning { get; } = Assembly.GetEntryAssembly()?.GetName().Name == GeneratorAssembly;
}
