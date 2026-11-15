using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;

namespace HireLab.Api.Infrastructure;

internal static class DataProtectionExtensions
{
    private const string ApplicationName = "HireLab";
    private const string KeyVaultKeyUriSetting = "DataProtection:KeyVaultKeyUri";
    private const string KeyRingBlobName = "keys.xml";

    public static void AddHireLabDataProtection(this WebApplicationBuilder builder)
    {
        var dataProtection = builder.Services.AddDataProtection().SetApplicationName(ApplicationName);

        var keyVaultKeyUri = builder.Configuration[KeyVaultKeyUriSetting];
        if (string.IsNullOrWhiteSpace(keyVaultKeyUri))
        {
            return;
        }

        builder.AddKeyedAzureBlobContainerClient(
            ResourceNames.DataProtectionKeys,
            settings => settings.DisableHealthChecks = true);

        var credential = new DefaultAzureCredential();

        dataProtection
            .PersistKeysToAzureBlobStorage(services => services
                .GetRequiredKeyedService<BlobContainerClient>(ResourceNames.DataProtectionKeys)
                .GetBlobClient(KeyRingBlobName))
            .ProtectKeysWithAzureKeyVault(new Uri(keyVaultKeyUri), credential);
    }
}
