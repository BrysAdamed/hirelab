using System.Diagnostics;
using HireLab.ServiceDefaults;

namespace HireLab.Api.Tests;

public sealed class UrlRedactionProcessorTests
{
    [Fact]
    public void Candidate_link_token_is_redacted_from_the_path_and_the_full_url()
    {
        // Arrange
        using var activity = new Activity("fake-request");
        activity.SetTag("url.path", "/s/fake-token_123");
        activity.SetTag("url.full", "https://hirelab.example.com/s/fake-token_123?fake=1");
        activity.SetTag("url.query", "fake=1");
        using var processor = new UrlRedactionProcessor();

        // Act
        processor.OnEnd(activity);

        // Assert
        activity.GetTagItem("url.path").ShouldBe("/s/{token}");
        activity.GetTagItem("url.full").ShouldBe("https://hirelab.example.com/s/{token}");
        activity.GetTagItem("url.query").ShouldBeNull();
    }

    [Theory]
    [InlineData("/s/")]
    [InlineData("/s/main-ABCD1234.js")]
    [InlineData("/s/assets/fake")]
    [InlineData("/api/candidate/session")]
    [InlineData("/sessions/fake")]
    public void Other_paths_are_kept(string path)
    {
        // Act
        var redacted = UrlRedactionProcessor.RedactPath(path);

        // Assert
        redacted.ShouldBe(path);
    }
}
