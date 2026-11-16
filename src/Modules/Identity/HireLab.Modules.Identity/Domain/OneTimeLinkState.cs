using System.Text.Json.Serialization;

namespace HireLab.Modules.Identity.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<OneTimeLinkState>))]
internal enum OneTimeLinkState
{
    Pending = 1,
    Used = 2,
    Revoked = 3,
    Expired = 4,
}
