using HireLab.Modules.Identity.Bootstrap;
using Microsoft.Extensions.Options;

namespace HireLab.Modules.Identity.Tests.Bootstrap;

public sealed class AdminBootstrapSettingsTests(HireLabTestHost host) : IntegrationTest(host)
{
    [Fact]
    public async Task The_public_base_url_is_required_outside_Development()
    {
        // Arrange
        var settings = MigrationJob.DefaultSettings();
        settings.Remove("HireLab:PublicBaseUrl");
        await using var job = MigrationJob.Create(Host, settings);

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => job.GetRequiredService<IOptions<StaffAppOptions>>().Value);

        // Assert
        exception.Message.ShouldContain("HireLab:PublicBaseUrl is required outside Development");
    }

    [Theory]
    [InlineData("https://hirelab.example.com/staff")]
    [InlineData("https://hirelab.example.com/?x=1")]
    [InlineData("/invite")]
    [InlineData("ftp://hirelab.example.com")]
    public async Task The_public_base_url_must_be_an_http_origin(string url)
    {
        // Arrange
        var settings = MigrationJob.DefaultSettings();
        settings["HireLab:PublicBaseUrl"] = url;
        await using var job = MigrationJob.Create(Host, settings);

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => job.GetRequiredService<IOptions<StaffAppOptions>>().Value);

        // Assert
        exception.Message.ShouldContain("must be an absolute http or https origin");
    }

    [Fact]
    public async Task The_admin_email_is_required_outside_Development()
    {
        // Arrange
        var settings = MigrationJob.DefaultSettings();
        settings.Remove("AdminBootstrap:Email");
        await using var job = MigrationJob.Create(Host, settings);

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => job.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value);

        // Assert
        exception.Message.ShouldContain("AdminBootstrap:Email is required outside Development");
    }

    [Fact]
    public async Task The_admin_email_must_be_an_email_address()
    {
        // Arrange
        var settings = MigrationJob.DefaultSettings(adminEmail: "not an email");
        await using var job = MigrationJob.Create(Host, settings);

        // Act
        var exception = Should.Throw<OptionsValidationException>(() => job.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value);

        // Assert
        exception.Message.ShouldContain("AdminBootstrap:Email is not a valid email address");
    }

    [Fact]
    public async Task Development_needs_neither_setting()
    {
        // Arrange
        await using var job = MigrationJob.Create(Host, new Dictionary<string, string?>(), environment: "Development");

        // Act
        var admin = job.GetRequiredService<IOptions<AdminBootstrapOptions>>().Value;
        var staffApp = job.GetRequiredService<IOptions<StaffAppOptions>>().Value;

        // Assert
        admin.Email.ShouldBeNull();
        admin.DisplayName.ShouldBe(AdminBootstrapOptions.DefaultDisplayName);
        admin.IssueNewLink.ShouldBeFalse();
        staffApp.PublicBaseUrl.ShouldBeNull();
    }
}
