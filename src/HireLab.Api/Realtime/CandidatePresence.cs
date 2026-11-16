namespace HireLab.Api.Realtime;

/// <summary>
/// Whether the candidate is connected to their session now, through how many connections (tabs or devices), and when
/// a connection last opened or closed (<c>null</c> before the first one). Held in memory, so a restart resets it (ADR 0006).
/// </summary>
public sealed record CandidatePresence(bool Connected, int Connections, DateTimeOffset? LastSeenAt);
