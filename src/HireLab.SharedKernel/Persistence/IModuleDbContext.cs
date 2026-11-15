using HireLab.SharedKernel.Handlers;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// Marks a module's <c>DbContext</c> (ADR 0004). Registered through <see cref="ModuleDbContextConventions"/>,
/// the context gets its schema as the default schema, its own <c>__EFMigrationsHistory</c> in that schema and
/// <c>snake_case</c> names, so the context itself declares only its schema.
/// </summary>
public interface IModuleDbContext : IUnitOfWork
{
    /// <summary>The module's PostgreSQL schema, for example <c>sessions</c>.</summary>
    static abstract string Schema { get; }
}
