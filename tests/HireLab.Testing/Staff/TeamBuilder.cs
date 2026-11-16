using HireLab.Modules.Identity.Domain;

namespace HireLab.Testing.Staff;

/// <summary>Builds a team with obviously fake values. Persist it with <see cref="HireLabTestHost.CreateTeamAsync"/>.</summary>
public sealed class TeamBuilder
{
    private static int _sequence;

    private string _name = $"Test Team {Interlocked.Increment(ref _sequence)}";

    /// <summary>Sets the team's name.</summary>
    public TeamBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    internal Team Build() => Team.Create(_name);
}
