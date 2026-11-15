using System.Diagnostics.CodeAnalysis;

namespace HireLab.SharedKernel.Results;

/// <summary>
/// The outcome of an operation without a value: success, or an expected failure carrying an <see cref="Results.Error"/>.
/// Exceptions are reserved for bugs and infrastructure failures.
/// </summary>
public class Result : IResultFactory<Result>
{
    private static readonly Result _success = new(null);

    protected Result(Error? error)
    {
        Error = error;
    }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => _success;

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.FromValue(value);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// The outcome of an operation that produces a <typeparamref name="TValue"/> on success.
/// </summary>
public sealed class Result<TValue> : Result, IResultFactory<Result<TValue>>
{
    private readonly TValue? _value;

    private Result(TValue value)
        : base(null)
    {
        _value = value;
    }

    private Result(Error error)
        : base(error)
    {
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result can't be accessed.");

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator Result<TValue>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<TValue>(error);
    }

    static Result<TValue> IResultFactory<Result<TValue>>.Failure(Error error) => error;

    internal static Result<TValue> FromValue(TValue value) => new(value);
}
