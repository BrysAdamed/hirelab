using Aspire.Hosting.JavaScript;

namespace HireLab.AppHost;

internal static class AngularDevServerExtensions
{
    private const string WebWorkspacePath = "../web";

    public static IResourceBuilder<JavaScriptAppResource> AddAngularDevServer(
        this IDistributedApplicationBuilder builder,
        string project,
        int port,
        IResourceBuilder<ProjectResource> api,
        string basePath = "/")
    {
        var devServer = builder.AddJavaScriptApp(project, WebWorkspacePath, $"start:{project}")
            .WithHttpEndpoint(port: port, env: "PORT")
            .WithUrlForEndpoint("http", url => url.Url = basePath)
            .WithEnvironment("API_URL", api.GetEndpoint("http"))
            .WaitFor(api);

        var endpoint = devServer.GetEndpoint("http");

        return devServer.WithArgs(context =>
        {
            context.Args.Add("--");
            context.Args.Add("--port");
            context.Args.Add(endpoint.Property(EndpointProperty.TargetPort));
        });
    }

    public static IResourceBuilder<JavaScriptAppResource> WaitForNpmInstallOf(
        this IResourceBuilder<JavaScriptAppResource> devServer,
        IResourceBuilder<JavaScriptAppResource> installingDevServer)
    {
        if (!installingDevServer.Resource.TryGetLastAnnotation<JavaScriptPackageInstallerAnnotation>(out var installer))
        {
            return devServer;
        }

        return devServer.WaitForCompletion(devServer.ApplicationBuilder.CreateResourceBuilder(installer.Resource));
    }
}
