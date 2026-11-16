using Microsoft.Extensions.Hosting;
using Npgsql;

namespace HireLab.ServiceDefaults;

/// <summary>The PostgreSQL data source every HireLab service uses.</summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adds the <see cref="NpgsqlDataSource"/> for the <paramref name="connectionName"/> connection string, with
    /// GSS encryption turned off. HireLab never uses Kerberos, and Npgsql's default attempt blocks a thread pool
    /// thread in SSPI for seconds on every new connection on domain-joined Windows machines.
    /// </summary>
    public static TBuilder AddHireLabDatabase<TBuilder>(this TBuilder builder, string connectionName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddAzureNpgsqlDataSource(
            connectionName,
            configureDataSourceBuilder: dataSource =>
                dataSource.ConnectionStringBuilder.GssEncryptionMode = GssEncryptionMode.Disable);

        return builder;
    }
}
