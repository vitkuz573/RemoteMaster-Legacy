// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared;

public interface IResult
{
    bool IsSuccess { get; }

    bool IsFailure { get; }

    IEnumerable<string> ErrorMessages { get; }

    Exception Exception { get; }
}

/// <summary>
/// Represents a basic operation result.
/// </summary>
public class Result : IResult
{
    public bool IsSuccess { get; protected set; }

    public IEnumerable<string> ErrorMessages { get; protected set; }

    public Exception Exception { get; protected set; }

    protected Result(bool isSuccess, IEnumerable<string> errorMessages = null, Exception ex = null)
    {
        IsSuccess = isSuccess;
        ErrorMessages = errorMessages ?? Enumerable.Empty<string>();
        Exception = ex;
    }

    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Constructs a successful result.
    /// </summary>
    public static Result Success() => new(true);

    /// <summary>
    /// Constructs a failed result with an error message.
    /// </summary>
    public static Result Failure(string errorMessage, Exception ex = null)
        => new(false, new[] { errorMessage }, ex);

    /// <summary>
    /// Constructs a failed result with multiple error messages.
    /// </summary>
    public static Result Failure(IEnumerable<string> errorMessages, Exception ex = null)
        => new(false, errorMessages, ex);

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

    // Conversion operators for easier usage
    public static implicit operator Result(string errorMessage) => Failure(errorMessage);
    
    public static implicit operator Result(Exception ex)
    {
        if (ex == null)
        {
            throw new ArgumentNullException(nameof(ex));
        }

        return Failure(ex.Message, ex);
    }

    public static explicit operator bool(Result result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.IsSuccess;
    }

}

/// <summary>
/// Represents a typed operation result.
/// </summary>
public class Result<T> : Result
{
    public T Value { get; private set; }

    protected Result(bool isSuccess, T value = default, IEnumerable<string> errorMessages = null, Exception ex = null)
        : base(isSuccess, errorMessages, ex)
    {
        Value = value;
    }

    /// <summary>
    /// Constructs a successful result with a value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Constructs a failed result with an error message.
    /// </summary>
    public static new Result<T> Failure(string errorMessage, Exception ex = null)
        => new(false, default, new[] { errorMessage }, ex);

    /// <summary>
    /// Constructs a failed result with multiple error messages.
    /// </summary>
    public static Result<T> Failure(IEnumerable<string> errorMessages, Exception ex = null)
        => new(false, default, errorMessages, ex);

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
    /// Combines multiple results into one.
    /// </summary>
    public static Result<IEnumerable<T>> Combine(params Result<T>[] results)
    {
        var failedResults = results.Where(r => r.IsFailure).ToList();
       
        if (failedResults.Any())
        {
            var allErrors = failedResults.SelectMany(r => r.ErrorMessages);
            
            return Result<IEnumerable<T>>.Failure(allErrors);
        }

        return Result<IEnumerable<T>>.Success(results.Select(r => r.Value));
    }

    // Conversion operators for easier usage
    public static implicit operator Result<T>(string errorMessage) => Failure(errorMessage);
    
    public static implicit operator Result<T>(Exception ex)
    {
        if (ex == null)
        {
            throw new ArgumentNullException(nameof(ex));
        }

        return Failure(ex.Message, ex);
    }

    public static explicit operator bool(Result<T> result)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.IsSuccess;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
