using System.Net.Http.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace HireLab.Api.Tests.Auth;

internal sealed record GroupEndpoint(string Method, string Pattern, Uri Uri, IReadOnlyList<string> Policies, bool AllowsAnonymous, bool AcceptsForm)
{
    public const string StaffGroup = "/api/staff";
    public const string CandidateGroup = "/api/candidate";

    private const string MultipartFormData = "multipart/form-data";
    private const string SampleGuid = "0190a5c4-7e3d-7b8a-9f21-3c4d5e6f7a8b";

    public static IReadOnlyList<GroupEndpoint> InGroup(IServiceProvider services, string group) =>
        services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => IsInGroup(endpoint.RoutePattern, group))
            .SelectMany(endpoint => MethodsOf(endpoint).Select(method => new GroupEndpoint(
                method,
                endpoint.RoutePattern.RawText ?? string.Empty,
                ToUri(endpoint.RoutePattern),
                PoliciesOf(endpoint),
                endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null,
                endpoint.Metadata.GetMetadata<IAcceptsMetadata>()?.ContentTypes.Contains(MultipartFormData) == true)))
            .ToList();

    /// <summary>
    /// A request to the endpoint; an unsafe method sends an empty JSON object, or an empty form to an endpoint that takes
    /// one, so endpoints with a body are reached.
    /// </summary>
    public HttpRequestMessage CreateRequest() => new(new HttpMethod(Method), Uri)
    {
        Content = HttpMethods.IsGet(Method) || HttpMethods.IsHead(Method) ? null
            : AcceptsForm ? new MultipartFormDataContent()
            : JsonContent.Create(new { }),
    };

    public override string ToString() => $"{Method} {Pattern}";

    private static bool IsInGroup(RoutePattern pattern, string group) =>
        pattern.RawText is { } rawText
        && (rawText.Equals(group, StringComparison.OrdinalIgnoreCase)
            || rawText.StartsWith(group + "/", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> MethodsOf(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods is { Count: > 0 } methods
            ? methods
            : [HttpMethods.Get];

    private static List<string> PoliciesOf(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(data => data.Policy).OfType<string>().ToList();

    private static Uri ToUri(RoutePattern pattern)
    {
        var segments = pattern.PathSegments.Select(segment => string.Concat(segment.Parts.Select(part => part switch
        {
            RoutePatternLiteralPart literal => literal.Content,
            RoutePatternSeparatorPart separator => separator.Content,
            RoutePatternParameterPart parameter => SampleValue(parameter),
            _ => string.Empty,
        })));

        return new Uri("/" + string.Join('/', segments), UriKind.Relative);
    }

    private static string SampleValue(RoutePatternParameterPart parameter)
    {
        var constraints = parameter.ParameterPolicies.Select(policy => policy.Content).ToList();

        return constraints switch
        {
            _ when constraints.Contains("guid") => SampleGuid,
            _ when constraints.Contains("int") || constraints.Contains("long") => "1",
            _ when constraints.Contains("bool") => "true",
            _ when constraints.Contains("datetime") => "2026-01-05",
            _ => "sample",
        };
    }
}
