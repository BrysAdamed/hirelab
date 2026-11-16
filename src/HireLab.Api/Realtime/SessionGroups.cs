using HireLab.Modules.Sessions.Contracts;

namespace HireLab.Api.Realtime;

/// <summary>The hub group of a session's connections. Each hub has its own groups, so both hubs use the same name.</summary>
internal static class SessionGroups
{
    public static string Of(InterviewSessionId sessionId) => $"session:{sessionId}";
}
