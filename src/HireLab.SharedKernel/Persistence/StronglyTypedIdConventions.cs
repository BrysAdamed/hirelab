using HireLab.SharedKernel.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HireLab.SharedKernel.Persistence;

/// <summary>Value converters that store the SharedKernel's strongly typed ids as <c>uuid</c> (ADR 0004).</summary>
public static class StronglyTypedIdConventions
{
    /// <summary>
    /// Maps every <see cref="UserId"/> and <see cref="TeamId"/> property to its <c>Guid</c>. Call it from the module
    /// DbContext's <c>ConfigureConventions</c>.
    /// </summary>
    public static ModelConfigurationBuilder AddSharedKernelIdConversions(this ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<UserId>().HaveConversion<UserIdConverter>();
        configurationBuilder.Properties<TeamId>().HaveConversion<TeamIdConverter>();

        return configurationBuilder;
    }

    private sealed class UserIdConverter() : ValueConverter<UserId, Guid>(id => id.Value, value => new UserId(value));

    private sealed class TeamIdConverter() : ValueConverter<TeamId, Guid>(id => id.Value, value => new TeamId(value));
}
