using HireLab.Modules.Identity.Domain;
using HireLab.SharedKernel.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Persistence;

internal sealed class IdentityModuleDbContext(DbContextOptions<IdentityModuleDbContext> options)
    : IdentityUserContext<User, UserId>(options), IModuleDbContext
{
    public static string Schema => "identity";

    public DbSet<Team> Teams => Set<Team>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.AddSharedKernelIdConversions();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityUserClaim<UserId>>(claim =>
        {
            claim.ToTable("user_claims");
            claim.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).HasConstraintName("fk_user_claims_users_user_id");
        });
        builder.Entity<IdentityUserLogin<UserId>>(login =>
        {
            login.ToTable("user_logins");
            login.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).HasConstraintName("fk_user_logins_users_user_id");
        });
        builder.Entity<IdentityUserToken<UserId>>(token =>
        {
            token.ToTable("user_tokens");
            token.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).HasConstraintName("fk_user_tokens_users_user_id");
        });

        builder.ApplyConfigurationsFromAssembly(typeof(IdentityModuleDbContext).Assembly);
    }
}
