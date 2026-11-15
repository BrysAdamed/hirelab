using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVault;

namespace HireLab.AppHost.Azure;

internal static class AzureProduction
{
    public const string ApiPrincipalNameVariable = "Migrations__ApiPrincipalName";
    public const string ApiPrincipalIdVariable = "Migrations__ApiPrincipalId";

    public static AzureProductionResources AddAzureProduction(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api,
        IResourceBuilder<AzurePostgresFlexibleServerResource> postgres,
        IResourceBuilder<AzureBlobStorageContainerResource> dataProtectionKeys)
    {
        var logs = builder.AddAzureLogAnalyticsWorkspace("logs")
            .WithDailyCap();

        builder.AddAzureContainerAppEnvironment("aca")
            .WithAzureLogAnalyticsWorkspace(logs)
            .WithDashboard(false);

        var insights = builder.AddAzureApplicationInsights("insights", logs);

        var keyVault = builder.AddAzureKeyVault("keyvault")
            .WithDataProtectionKey();

        var apiIdentity = builder.AddAzureUserAssignedIdentity("api-identity");
        var migrationsIdentity = builder.AddAzureUserAssignedIdentity("migrations-identity");

        postgres.WithEntraAdministrator(migrationsIdentity);

        api.WithAzureUserAssignedIdentity(apiIdentity)
            .WithReference(insights)
            .WithReference(dataProtectionKeys)
            .WithRoleAssignments(keyVault, KeyVaultBuiltInRole.KeyVaultCryptoServiceEncryptionUser)
            .WithEnvironment(DataProtectionKey.KeyUriEnvironmentVariable, new BicepOutputReference(DataProtectionKey.UriOutput, keyVault.Resource))
            .PublishAsAzureContainerApp((_, app) => app.WithApiSettings());

        return new AzureProductionResources(apiIdentity, migrationsIdentity, insights);
    }

    public static IResourceBuilder<T> PublishAsMigrationJob<T>(
        this IResourceBuilder<T> migrations,
        AzureProductionResources azure)
        where T : IComputeResource, IResourceWithEnvironment =>
        migrations
            .WithAzureUserAssignedIdentity(azure.MigrationsIdentity)
            .WithReference(azure.Insights)
            .WithEnvironment(ApiPrincipalNameVariable, azure.ApiIdentity.Resource.PrincipalName)
            .WithEnvironment(ApiPrincipalIdVariable, azure.ApiIdentity.Resource.PrincipalId)
            .PublishAsAzureContainerAppJob();
}

internal sealed record AzureProductionResources(
    IResourceBuilder<AzureUserAssignedIdentityResource> ApiIdentity,
    IResourceBuilder<AzureUserAssignedIdentityResource> MigrationsIdentity,
    IResourceBuilder<AzureApplicationInsightsResource> Insights);
