using HireLab.SharedKernel.Results;

namespace HireLab.SharedKernel.Handlers;

/// <summary>
/// Handles a use case that only reads. Registered through
/// <see cref="HandlerRegistration.AddHandlers{TUnitOfWork}"/>, which wraps it in logging and validation.
/// </summary>
/// <typeparam name="TQuery">The query.</typeparam>
/// <typeparam name="TResult"><see cref="Result"/> or <see cref="Result{TValue}"/>.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : notnull
    where TResult : Result, IResultFactory<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
