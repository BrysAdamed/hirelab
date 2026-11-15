using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// The conventions every module <c>DbContext</c> shares (ADR 0004): PostgreSQL through the host's
/// <see cref="NpgsqlDataSource"/>, the module schema as the default schema, <c>__EFMigrationsHistory</c>
/// in that schema and <c>snake_case</c> names.
/// </summary>
public static class ModuleDbContextConventions
{
    /// <summary>
    /// Registers <typeparamref name="TContext"/> as a scoped service on the <see cref="NpgsqlDataSource"/>
    /// that the host registers.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext, IModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDbContext<TContext>((provider, options) =>
            options.UseModuleConventions<TContext>(npgsql =>
                npgsql.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>(), ConfigureNpgsql<TContext>)));
    }

    /// <summary>Applies the module conventions with a plain connection string, for design-time tooling.</summary>
    public static DbContextOptionsBuilder UseModuleConventions<TContext>(this DbContextOptionsBuilder options, string connectionString)
        where TContext : DbContext, IModuleDbContext =>
        options.UseModuleConventions<TContext>(npgsql => npgsql.UseNpgsql(connectionString, ConfigureNpgsql<TContext>));

    private static DbContextOptionsBuilder UseModuleConventions<TContext>(
        this DbContextOptionsBuilder options,
        Action<DbContextOptionsBuilder> useNpgsql)
        where TContext : DbContext, IModuleDbContext
    {
        ArgumentNullException.ThrowIfNull(options);

        useNpgsql(options);

        return options
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCustomizer, ModuleModelCustomizer<TContext>>();
    }

    private static void ConfigureNpgsql<TContext>(NpgsqlDbContextOptionsBuilder npgsql)
        where TContext : DbContext, IModuleDbContext =>
        npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, TContext.Schema);
}
