using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.OperationalInsights;
using Azure.Provisioning.PostgreSql;
using Azure.Provisioning.Storage;

namespace HireLab.AppHost.Azure;

internal static class AzureResourceSettings
{
    public const string PostgresMajorVersion = "17";
    public const string PostgresContainerImageTag = PostgresMajorVersion;
    public const string PostgresDataVolumeName = "hirelab-postgres-" + PostgresMajorVersion + "-data";

    private const double LogAnalyticsDailyCapInGb = 0.2;
    private const int PostgresStorageSizeInGb = 32;
    private const int PostgresBackupRetentionDays = 7;
    private const string AutoConfigureDataProtectionProperty = "AutoConfigureDataProtection";

    public static IResourceBuilder<AzureLogAnalyticsWorkspaceResource> WithDailyCap(
        this IResourceBuilder<AzureLogAnalyticsWorkspaceResource> builder) =>
        builder.ConfigureInfrastructure(infrastructure =>
        {
            var workspace = infrastructure.GetProvisionableResources().OfType<OperationalInsightsWorkspace>().Single();
            workspace.WorkspaceCapping = new OperationalInsightsWorkspaceCapping { DailyQuotaInGB = LogAnalyticsDailyCapInGb };
        });

    public static IResourceBuilder<AzurePostgresFlexibleServerResource> WithHireLabSettings(
        this IResourceBuilder<AzurePostgresFlexibleServerResource> builder) =>
        builder.ConfigureInfrastructure(infrastructure =>
        {
            var server = infrastructure.GetProvisionableResources().OfType<PostgreSqlFlexibleServer>().Single();
            server.Version = new BicepValue<PostgreSqlFlexibleServerVersion>(new StringLiteralExpression(PostgresMajorVersion));
            server.Sku = new PostgreSqlFlexibleServerSku
            {
                Name = "Standard_B1ms",
                Tier = PostgreSqlFlexibleServerSkuTier.Burstable,
            };
            server.Storage = new PostgreSqlFlexibleServerStorage { StorageSizeInGB = PostgresStorageSizeInGb };
            server.AuthConfig = new PostgreSqlFlexibleServerAuthConfig
            {
                ActiveDirectoryAuth = PostgreSqlFlexibleServerActiveDirectoryAuthEnum.Enabled,
                PasswordAuth = PostgreSqlFlexibleServerPasswordAuthEnum.Disabled,
            };
            server.Backup = new PostgreSqlFlexibleServerBackupProperties
            {
                BackupRetentionDays = PostgresBackupRetentionDays,
                GeoRedundantBackup = PostgreSqlFlexibleServerGeoRedundantBackupEnum.Disabled,
            };
            server.HighAvailability = new PostgreSqlFlexibleServerHighAvailability
            {
                Mode = PostgreSqlFlexibleServerHighAvailabilityMode.Disabled,
            };
        });

    public static IResourceBuilder<AzurePostgresFlexibleServerResource> WithEntraAdministrator(
        this IResourceBuilder<AzurePostgresFlexibleServerResource> builder,
        IResourceBuilder<AzureUserAssignedIdentityResource> identity) =>
        builder.ConfigureInfrastructure(infrastructure =>
        {
            var server = infrastructure.GetProvisionableResources().OfType<PostgreSqlFlexibleServer>().Single();
            var principalId = identity.Resource.PrincipalId.AsProvisioningParameter(infrastructure, "migrationsPrincipalId");
            var principalName = identity.Resource.PrincipalName.AsProvisioningParameter(infrastructure, "migrationsPrincipalName");

            infrastructure.Add(new PostgreSqlFlexibleServerActiveDirectoryAdministrator("migrationsAdministrator")
            {
                Parent = server,
                Name = principalId,
                PrincipalType = PostgreSqlFlexibleServerPrincipalType.ServicePrincipal,
                PrincipalName = principalName,
            });
        });

    public static IResourceBuilder<AzureStorageResource> WithHireLabSettings(
        this IResourceBuilder<AzureStorageResource> builder) =>
        builder.ConfigureInfrastructure(infrastructure =>
        {
            var account = infrastructure.GetProvisionableResources().OfType<StorageAccount>().Single();
            account.Sku = new StorageSku { Name = StorageSkuName.StandardLrs };
            account.AllowSharedKeyAccess = false;
            account.AllowBlobPublicAccess = false;
            account.MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2;

            var blobService = infrastructure.GetProvisionableResources().OfType<BlobService>().Single();
            blobService.DeleteRetentionPolicy = new DeleteRetentionPolicy { IsEnabled = false };
            blobService.ContainerDeleteRetentionPolicy = new DeleteRetentionPolicy { IsEnabled = false };
            blobService.IsVersioningEnabled = false;
        });

    public static IResourceBuilder<AzureKeyVaultResource> WithDataProtectionKey(
        this IResourceBuilder<AzureKeyVaultResource> builder) =>
        builder.ConfigureInfrastructure(infrastructure =>
        {
            var vault = infrastructure.GetProvisionableResources().OfType<KeyVaultService>().Single();
            var key = new KeyVaultKey("dataProtectionKey")
            {
                Parent = vault,
                Name = DataProtectionKey.Name,
                KeyType = "RSA",
                KeySize = 2048,
            };
            infrastructure.Add(key);
            infrastructure.Add(new ProvisioningOutput(DataProtectionKey.UriOutput, typeof(string)) { Value = key.KeyUri });
        });

    public static void WithApiSettings(this ContainerApp app)
    {
        app.Template.Scale.MinReplicas = 0;
        app.Template.Scale.MaxReplicas = 1;

        if (app.Configuration.ProvisionableProperties.TryGetValue(AutoConfigureDataProtectionProperty, out var value)
            && value is BicepValue<bool> autoConfigureDataProtection)
        {
            autoConfigureDataProtection.Assign(false);
        }
    }
}
