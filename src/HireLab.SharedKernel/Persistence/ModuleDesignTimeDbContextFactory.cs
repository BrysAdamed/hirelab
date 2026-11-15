using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create a module's <c>DbContext</c> with the module conventions, using the module
/// project as its own startup project. A module declares an empty subclass of it.
/// </summary>
public abstract class ModuleDesignTimeDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext, IModuleDbContext
{
    private const string DesignTimeConnectionString = "Host=localhost;Database=hirelab";

    /// <inheritdoc />
    public TContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseModuleConventions<TContext>(DesignTimeConnectionString)
            .Options;

        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}
