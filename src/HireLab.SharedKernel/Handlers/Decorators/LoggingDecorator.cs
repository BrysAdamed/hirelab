using System.Diagnostics;
using HireLab.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace HireLab.SharedKernel.Handlers.Decorators;

internal static partial class LoggingDecorator
{
    internal sealed class Command<TCommand, TResult>(ICommandHandler<TCommand, TResult> inner, ILogger logger)
        : ICommandHandler<TCommand, TResult>
        where TCommand : notnull
        where TResult : Result, IResultFactory<TResult>
    {
        public Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TCommand).Name, () => inner.HandleAsync(command, cancellationToken));
    }

    internal sealed class Query<TQuery, TResult>(IQueryHandler<TQuery, TResult> inner, ILogger logger)
        : IQueryHandler<TQuery, TResult>
        where TQuery : notnull
        where TResult : Result, IResultFactory<TResult>
    {
        public Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TQuery).Name, () => inner.HandleAsync(query, cancellationToken));
    }

    private static async Task<TResult> LogAsync<TResult>(ILogger logger, string requestName, Func<Task<TResult>> next)
        where TResult : Result
    {
        LogHandling(logger, requestName);
        var startedAt = Stopwatch.GetTimestamp();

        var result = await next();

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        if (result.IsSuccess)
        {
            LogSucceeded(logger, requestName, elapsedMilliseconds);
        }
        else
        {
            LogFailed(logger, requestName, result.Error.Code, result.Error.Type, elapsedMilliseconds);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling {RequestName}")]
    private static partial void LogHandling(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName} in {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogSucceeded(ILogger logger, string requestName, double elapsedMilliseconds);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{RequestName} failed with {ErrorCode} ({ErrorType}) in {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogFailed(
        ILogger logger,
        string requestName,
        string errorCode,
        ErrorType errorType,
        double elapsedMilliseconds);
}
