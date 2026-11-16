namespace HireLab.SharedKernel.Persistence;

/// <summary>
/// Marks a system job (ADR 0009): work that runs outside any user's request, on behalf of no team, such as the orphan
/// attachment sweep. Only the migration worker, hosted services and types marked with this interface may bypass the
/// <see cref="TeamQueryFilter"/> with <c>IgnoreQueryFilters</c>; an architecture test enforces it.
/// </summary>
public interface ISystemJob;
