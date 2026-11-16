using HireLab.SharedKernel.Events;

namespace HireLab.Modules.Sessions.Contracts;

/// <summary>
/// Raised after a candidate saved the response to a session task (ADR 0031). <see cref="Revision"/> is the response's
/// revision after the save; it carries no response content, and readers refetch the session task over HTTP (ADR 0006).
/// </summary>
public sealed record ResponseUpdated(InterviewSessionId SessionId, SessionTaskId SessionTaskId, int Revision) : IIntegrationEvent;
