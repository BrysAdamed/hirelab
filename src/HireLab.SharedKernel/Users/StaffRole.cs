using System.Text.Json.Serialization;

namespace HireLab.SharedKernel.Users;

/// <summary>The fixed role of a staff user (ADR 0009, ADR 0020). Sent over the wire by name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StaffRole>))]
public enum StaffRole
{
    /// <summary>Manages users and teams only. Has no team and sees no candidate data. There is exactly one.</summary>
    Admin = 1,

    /// <summary>Runs one team. A team has at most one active manager.</summary>
    Manager = 2,

    /// <summary>Works on the positions of exactly one team.</summary>
    Interviewer = 3,
}
