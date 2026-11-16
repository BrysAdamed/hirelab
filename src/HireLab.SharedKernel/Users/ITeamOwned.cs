namespace HireLab.SharedKernel.Users;

/// <summary>
/// Marks an entity that belongs to one team (ADR 0009). Every module DbContext gives such an entity the
/// <see cref="Persistence.TeamQueryFilter">team query filter</see>, so a query returns only the current user's team's
/// rows, and none for the admin. Implement it on aggregate roots with a public <see cref="TeamId"/> property.
/// </summary>
public interface ITeamOwned
{
    /// <summary>The team the entity belongs to.</summary>
    TeamId TeamId { get; }
}
