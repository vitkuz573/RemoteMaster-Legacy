namespace RemoteMaster.Shared;

/// <summary>
/// Interface for operation result.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    IReadOnlyCollection<string> ErrorMessages { get; }

    Exception? Exception { get; }
}

/// <summary>
/// Represents a basic operation result.
/// </summary>
public class Result : IResult
{
    public bool IsSuccess { get; protected set; }

    public IReadOnlyCollection<string> ErrorMessages { get; protected set; } = Array.Empty<string>();

    public Exception? Exception { get; protected set; }

    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, IEnumerable<string> errorMessages = null, Exception ex = null)
    {
        IsSuccess = isSuccess;
        ErrorMessages = errorMessages?.ToArray() ?? Array.Empty<string>();
        Exception = ex;
    }

    /// <summary>
    /// Constructs a successful result.
    /// </summary>
    public static Result Success() => new Result(true);

    /// <summary>
    /// Constructs a failed result with an error message.
    /// </summary>
    public static Result Failure(string errorMessage, Exception ex = null)
        => new Result(false, new[] { errorMessage }, ex);

    /// <summary>
    /// Constructs a failed result with multiple error messages.
    /// </summary>
    public static Result Failure(IEnumerable<string> errorMessages, Exception ex = null)
        => new Result(false, errorMessages, ex);

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsSuccess)
        {
            action();
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result OnFailure(Action<IEnumerable<string>, Exception> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsFailure)
        {
            action(ErrorMessages, Exception);
        }

        return this;
    }
}

/// <summary>
/// Represents a typed operation result.
/// </summary>
public class Result<T> : IResult
{
    public T Value { get; private set; }

    public bool IsSuccess { get; private set; }

    public IReadOnlyCollection<string> ErrorMessages { get; private set; } = Array.Empty<string>();

    public Exception? Exception { get; private set; }

    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T value = default, IEnumerable<string> errorMessages = null, Exception ex = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessages = errorMessages?.ToArray() ?? Array.Empty<string>();
        Exception = ex;
    }

    /// <summary>
    /// Constructs a successful result with a value.
    /// </summary>
    public static Result<T> Success(T value) => new Result<T>(true, value);

    /// <summary>
    /// Constructs a failed result with an error message.
    /// </summary>
    public static Result<T> Failure(string errorMessage, Exception ex = null)
        => new Result<T>(false, default, new[] { errorMessage }, ex);

    /// <summary>
    /// Constructs a failed result with multiple error messages.
    /// </summary>
    public static Result<T> Failure(IEnumerable<string> errorMessages, Exception ex = null)
        => new Result<T>(false, default, errorMessages, ex);

    /// <summary>
    /// Transforms the result value if successful.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        return IsFailure
            ? Result<TOut>.Failure(ErrorMessages, Exception)
            : Result<TOut>.Success(transform(Value));
    }

    /// <summary>
    /// Chains the result with a new operation if successful.
    /// </summary>
    public Result<TOut> AndThen<TOut>(Func<T, Result<TOut>> next)
    {
        if (next == null)
        {
            throw new ArgumentNullException(nameof(next));
        }

        return IsSuccess
            ? next(Value)
            : Result<TOut>.Failure(ErrorMessages, Exception);
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// </summary>
    public static Result<IEnumerable<T>> Combine(params Result<T>[] results)
    {
        var failedResults = results.Where(r => r.IsFailure).ToList();

        if (failedResults.Any())
        {
            var allErrors = failedResults.SelectMany(r => r.ErrorMessages);
            return Result<IEnumerable<T>>.Failure(allErrors);
        }
        else
        {
            return Result<IEnumerable<T>>.Success(results.Select(r => r.Value));
        }
    }
}
