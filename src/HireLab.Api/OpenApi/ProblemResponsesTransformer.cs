using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace HireLab.Api.OpenApi;

internal sealed class ProblemResponsesTransformer : IOpenApiDocumentTransformer
{
    private const string SchemaId = nameof(ProblemDetails);
    private const string ProblemContentType = "application/problem+json";
    private const string DefaultResponseKey = "default";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();

        if (!document.Components.Schemas.ContainsKey(SchemaId))
        {
            var schema = await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), cancellationToken: cancellationToken);
            document.AddComponent(SchemaId, schema);
        }

        var problemResponse = new OpenApiResponse
        {
            Description = "The request failed. The body describes the problem.",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [ProblemContentType] = new() { Schema = new OpenApiSchemaReference(SchemaId, document) },
            },
        };

        var operations = document.Paths.Values
            .Where(path => path.Operations is not null)
            .SelectMany(path => path.Operations!.Values);

        foreach (var operation in operations)
        {
            operation.Responses ??= [];
            operation.Responses.TryAdd(DefaultResponseKey, problemResponse);
        }
    }
}
