using System.Net;

namespace HireLab.Api.Tests;

public sealed class SpaHostingTests(HireLabApiFactory factory) : IClassFixture<HireLabApiFactory>
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";
    private const string NoCacheControl = "no-cache";

    [Theory]
    [InlineData("/")]
    [InlineData("/sessions")]
    [InlineData("/sessions/0190a5c4-7e3d-7b8a-9f21-3c4d5e6f7a8b/review")]
    public async Task Staff_route_serves_staff_index_without_caching(string path)
    {
        // Act
        var response = await GetAsync(path);

        // Assert
        await ShouldServeIndexAsync(response, "staff");
    }

    [Theory]
    [InlineData("/s")]
    [InlineData("/s/")]
    [InlineData("/s/fake-candidate-token")]
    [InlineData("/s/fake-candidate-token/done")]
    public async Task Candidate_route_serves_candidate_index_without_caching(string path)
    {
        // Act
        var response = await GetAsync(path);

        // Assert
        await ShouldServeIndexAsync(response, "candidate");
    }

    [Theory]
    [InlineData("/main-ABCD1234.js", "staff")]
    [InlineData("/s/main-WXYZ5678.js", "candidate")]
    public async Task Hashed_asset_is_cached_as_immutable(string path, string app)
    {
        // Act
        var response = await GetAsync(path);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe(ImmutableCacheControl);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain(app);
    }

    [Fact]
    public async Task Asset_without_hash_is_revalidated()
    {
        // Act
        var response = await GetAsync("/favicon.ico");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe(NoCacheControl);
    }

    [Theory]
    [InlineData("/main-MISSING1.js")]
    [InlineData("/s/main-MISSING1.js")]
    public async Task Missing_file_is_not_found_instead_of_index(string path)
    {
        // Act
        var response = await GetAsync(path);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/unknown")]
    [InlineData("/hubs/unknown")]
    [InlineData("/openapi/unknown.json")]
    [InlineData("/alive/unknown")]
    [InlineData("/health/unknown")]
    public async Task Server_path_never_falls_back_to_an_spa(string path)
    {
        // Act
        var response = await GetAsync(path);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/html");
    }

    [Fact]
    public async Task Liveness_endpoint_is_not_shadowed_by_an_spa()
    {
        // Act
        var response = await GetAsync("/alive");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe("Healthy");
    }

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        using var client = factory.CreateClient();
        return await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
    }

    private static async Task ShouldServeIndexAsync(HttpResponseMessage response, string app)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        response.Headers.CacheControl?.ToString().ShouldBe(NoCacheControl);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain($"<title>{app}</title>");
    }
}
