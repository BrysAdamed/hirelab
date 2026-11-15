namespace HireLab.SharedKernel.Events;

/// <summary>
/// A fact another module may react to. Defined in the raising module's <c>.Contracts</c> project
/// and carrying ids rather than personal data.
/// </summary>
public interface IIntegrationEvent;
