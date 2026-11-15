namespace HireLab.SharedKernel.Results;

/// <summary>
/// Lets generic code such as the handler decorators create a failed <typeparamref name="TSelf"/>
/// without knowing whether it is a <see cref="Result"/> or a <see cref="Result{TValue}"/>.
/// </summary>
public interface IResultFactory<TSelf>
    where TSelf : Result
{
    static abstract TSelf Failure(Error error);
}
