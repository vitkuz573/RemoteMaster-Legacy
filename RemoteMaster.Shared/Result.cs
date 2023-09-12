// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared;

/// <summary>
/// Interface for operation results.
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

    /// <summary>
    /// Protected constructor for base result.
    /// </summary>
    protected Result(bool isSuccess, IEnumerable<string> errorMessages = null, Exception ex = null)
    {
        IsSuccess = isSuccess;
        ErrorMessages = errorMessages?.ToArray() ?? Array.Empty<string>();
        Exception = ex;
    }

    /// <summary>
    /// Constructs a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Constructs a failed result with an error message.
    /// </summary>
    public static Result Failure(string errorMessage, Exception ex = null)
    {
        return new Result(false, new[] { errorMessage }, ex);
    }

    /// <summary>
    /// Constructs a failed result with multiple error messages.
    /// </summary>
    public static Result Failure(IEnumerable<string> errorMessages, Exception ex = null)
    {
        return new Result(false, errorMessages, ex);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result OnSuccess(Func<Result> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsSuccess)
        {
            return func();
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result OnFailure(Func<Result> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsFailure)
        {
            return func();
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Inspects the result without changing it.
    /// </summary>
    public Result Tap(Action<Result> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        action(this);

        return this;
    }

    /// <summary>
    /// Tries to execute a function and wraps the result or exception in a Result.
    /// </summary>
    public static Result Try(Func<Result> func, Func<Exception, string> errorHandler = null)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return Failure(errorHandler?.Invoke(ex) ?? ex.Message, ex);
        }
    }

    /// <summary>
    /// Asynchronously executes an action if the result is successful.
    /// </summary>
    public async Task<Result> OnSuccessAsync(Func<Task<Result>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsSuccess)
        {
            return await func();
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Asynchronously executes an action if the result is a failure.
    /// </summary>
    public async Task<Result> OnFailureAsync(Func<Task<Result>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsFailure)
        {
            return await func();
        }
        else
        {
            return this;
        }
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

    /// <summary>
    /// Private constructor for typed result.
    /// </summary>
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
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Constructs a failed result with an error message.
    /// </summary>
    public static Result<T> Failure(string errorMessage, Exception ex = null)
    {
        return new Result<T>(false, default, new[] { errorMessage }, ex);
    }

    /// <summary>
    /// Constructs a failed result with multiple error messages.
    /// </summary>
    public static Result<T> Failure(IEnumerable<string> errorMessages, Exception ex = null)
    {
        return new Result<T>(false, default, errorMessages, ex);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<T> OnSuccess(Func<T, Result<T>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsSuccess)
        {
            return func(Value);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result<T> OnFailure(Func<IReadOnlyCollection<string>, Exception, Result<T>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsFailure)
        {
            return func(ErrorMessages, Exception);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Inspects the result without changing it.
    /// </summary>
    public Result<T> Tap(Action<Result<T>> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        action(this);

        return this;
    }

    /// <summary>
    /// Tries to execute a function and wraps the result or exception in a Result<T>.
    /// </summary>
    public static Result<T> Try(Func<Result<T>> func, Func<Exception, string> errorHandler = null)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return Failure(errorHandler?.Invoke(ex) ?? ex.Message, ex);
        }
    }

    /// <summary>
    /// Asynchronously executes an action if the result is successful.
    /// </summary>
    public async Task<Result<T>> OnSuccessAsync(Func<T, Task<Result<T>>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsSuccess)
        {
            return await func(Value);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Asynchronously executes an action if the result is a failure.
    /// </summary>
    public async Task<Result<T>> OnFailureAsync(Func<IReadOnlyCollection<string>, Exception, Task<Result<T>>> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsFailure)
        {
            return await func(ErrorMessages, Exception);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// Converts the result's value using the provided converter.
    /// </summary>
    public Result<U> Convert<U>(Func<T, U> converter)
    {
        if (converter == null)
        {
            throw new ArgumentNullException(nameof(converter));
        }

        if (IsSuccess)
        {
            return Result<U>.Success(converter(Value));
        }
        else
        {
            return Result<U>.Failure(ErrorMessages, Exception);
        }
    }
}
