using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;

namespace HireLab.SharedKernel.Tests.Persistence;

public sealed class TeamQueryFilterTests
{
    [Fact]
    public void Team_owned_root_and_its_derived_types_get_the_filter_once()
    {
        // Arrange
        using var dbContext = Create<ScopedContext>(options => new ScopedContext(options));

        // Act
        var model = dbContext.Model;

        // Assert
        model.FindEntityType(typeof(TeamRow))!.FindDeclaredQueryFilter(TeamQueryFilter.Name).ShouldNotBeNull();
        model.FindEntityType(typeof(SpecialTeamRow))!.FindDeclaredQueryFilter(TeamQueryFilter.Name).ShouldBeNull();
        model.FindEntityType(typeof(PlainRow))!.GetDeclaredQueryFilters().ShouldBeEmpty();
    }

    [Fact]
    public void Team_owned_type_deriving_from_an_unowned_root_is_rejected()
    {
        // Arrange
        using var dbContext = Create<MisplacedContext>(options => new MisplacedContext(options));

        // Act and assert
        Should.Throw<InvalidOperationException>(() => dbContext.Model).Message.ShouldContain(nameof(TeamOwnedChild));
    }

    private static TContext Create<TContext>(Func<DbContextOptions<TContext>, TContext> create)
        where TContext : DbContext, IModuleDbContext
    {
        var options = new DbContextOptionsBuilder<TContext>();
        options.UseModuleConventions<TContext>("Host=localhost;Database=hirelab-model-only");
        return create(options.Options);
    }

    private class TeamRow : ITeamOwned
    {
        public Guid Id { get; set; }

        public TeamId TeamId { get; set; }
    }

    private sealed class SpecialTeamRow : TeamRow;

    private class PlainRow
    {
        public Guid Id { get; set; }
    }

    private sealed class TeamOwnedChild : PlainRow, ITeamOwned
    {
        public TeamId TeamId { get; set; }
    }

    private sealed class ScopedContext(DbContextOptions<ScopedContext> options) : DbContext(options), IModuleDbContext
    {
        public static string Schema => "scoped";

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
            configurationBuilder.AddSharedKernelIdConversions();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamRow>();
            modelBuilder.Entity<SpecialTeamRow>();
            modelBuilder.Entity<PlainRow>();
        }
    }

    private sealed class MisplacedContext(DbContextOptions<MisplacedContext> options) : DbContext(options), IModuleDbContext
    {
        public static string Schema => "misplaced";

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
            configurationBuilder.AddSharedKernelIdConversions();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlainRow>();
            modelBuilder.Entity<TeamOwnedChild>();
        }
    }
}
