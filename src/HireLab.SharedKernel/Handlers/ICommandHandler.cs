using HireLab.SharedKernel.Results;

namespace HireLab.SharedKernel.Handlers;

/// <summary>
/// Handles a use case that changes state. Registered through
/// <see cref="HandlerRegistration.AddHandlers{TUnitOfWork}"/>, which wraps it in logging, validation and the unit of work:
/// changes are saved and integration events dispatched only when the handler returns a successful result.
/// </summary>
/// <typeparam name="TCommand">The command.</typeparam>
/// <typeparam name="TResult"><see cref="Result"/> or <see cref="Result{TValue}"/>.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : notnull
    where TResult : Result, IResultFactory<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
