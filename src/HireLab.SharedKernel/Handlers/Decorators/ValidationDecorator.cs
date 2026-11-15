using FluentValidation;
using HireLab.SharedKernel.Results;

namespace HireLab.SharedKernel.Handlers.Decorators;

internal static class ValidationDecorator
{
    internal sealed class Command<TCommand, TResult>(
        ICommandHandler<TCommand, TResult> inner,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResult>
        where TCommand : notnull
        where TResult : Result, IResultFactory<TResult>
    {
        public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            var error = await ValidateAsync(validators, command, cancellationToken);
            return error is null ? await inner.HandleAsync(command, cancellationToken) : TResult.Failure(error);
        }
    }

    internal sealed class Query<TQuery, TResult>(
        IQueryHandler<TQuery, TResult> inner,
        IEnumerable<IValidator<TQuery>> validators)
        : IQueryHandler<TQuery, TResult>
        where TQuery : notnull
        where TResult : Result, IResultFactory<TResult>
    {
        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
        {
            var error = await ValidateAsync(validators, query, cancellationToken);
            return error is null ? await inner.HandleAsync(query, cancellationToken) : TResult.Failure(error);
        }
    }

    private static async Task<ValidationError?> ValidateAsync<TRequest>(
        IEnumerable<IValidator<TRequest>> validators,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = new List<FluentValidation.Results.ValidationFailure>();

        foreach (var validator in validators)
        {
            var validationResult = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(validationResult.Errors);
        }

        if (failures.Count == 0)
        {
            return null;
        }

        var errors = failures
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return new ValidationError(errors);
    }
}
