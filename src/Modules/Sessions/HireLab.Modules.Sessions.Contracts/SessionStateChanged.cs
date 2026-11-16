using HireLab.SharedKernel.Events;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Raised after a session went Live or finished (ADR 0031). Thin, as real-time events are (ADR 0006): readers refetch
/// the session over HTTP.
/// </summary>
public sealed record SessionStateChanged(InterviewSessionId SessionId) : IIntegrationEvent;
