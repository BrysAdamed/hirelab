using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HireLab.Api.Tests;

public sealed class DataProtectionTests(HireLabApiFactory factory) : IClassFixture<HireLabApiFactory>
{
    [Fact]
    public void Keys_use_the_default_store_when_no_key_vault_key_is_configured()
    {
        // Act
        var options = factory.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        // Assert
        options.XmlRepository.ShouldBeNull();
        options.XmlEncryptor.ShouldBeNull();
    }

    [Fact]
    public void Keys_are_persisted_to_blob_storage_and_wrapped_with_the_key_vault_key_in_azure()
    {
        // Arrange
        using var azureFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DataProtection:KeyVaultKeyUri", "https://hirelab-example.vault.azure.net/keys/data-protection");
            builder.UseSetting(
                "ConnectionStrings:data-protection-keys",
                "Endpoint=https://hirelabexample.blob.core.windows.net;ContainerName=data-protection-keys");
            builder.ConfigureTestServices(services => services.RemoveAll<IHostedService>());
        });

        // Act
        var options = azureFactory.Services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        // Assert
        options.XmlRepository.ShouldNotBeNull().GetType().Name.ShouldBe("AzureBlobXmlRepository");
        options.XmlEncryptor.ShouldNotBeNull();
    }
}
