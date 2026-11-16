using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVault;

namespace HireLab.AppHost.Azure;

internal static class AzureProduction
{
    public const string ApiPrincipalNameVariable = "Migrations__ApiPrincipalName";
    public const string ApiPrincipalIdVariable = "Migrations__ApiPrincipalId";
    public const string PublicBaseUrlVariable = "HireLab__PublicBaseUrl";
    public const string AdminEmailVariable = "AdminBootstrap__Email";
    public const string AdminEmailParameter = "admin-email";

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

        var adminEmail = builder.AddParameter(AdminEmailParameter)
            .WithDescription("The email the first admin logs in with. The migration job issues the admin invite link for it.");

        return new AzureProductionResources(api, apiIdentity, migrationsIdentity, insights, adminEmail);
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
            .WithEnvironment(PublicBaseUrlVariable, azure.Api.GetEndpoint("https"))
            .WithEnvironment(AdminEmailVariable, azure.AdminEmail)
            .PublishAsAzureContainerAppJob();
}

internal sealed record AzureProductionResources(
    IResourceBuilder<ProjectResource> Api,
    IResourceBuilder<AzureUserAssignedIdentityResource> ApiIdentity,
    IResourceBuilder<AzureUserAssignedIdentityResource> MigrationsIdentity,
    IResourceBuilder<AzureApplicationInsightsResource> Insights,
    IResourceBuilder<ParameterResource> AdminEmail);
