using HireLab.SharedKernel.Users;

namespace HireLab.Modules.Identity.Domain;

internal sealed class Team
{
    public const int NameMaxLength = 100;

    private Team()
    {
    }

    public TeamId Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public uint Version { get; private set; }

    public static Team Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Team { Id = TeamId.New(), Name = name.Trim() };
    }
}
