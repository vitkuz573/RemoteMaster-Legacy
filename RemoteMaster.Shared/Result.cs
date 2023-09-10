// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared;

public class Result<T>
{
    public bool IsSuccess { get; private set; }

    public T Value { get; private set; }

    public string ErrorMessage { get; private set; }

    public Exception Exception { get; private set; }

    protected Result(bool isSuccess, T value = default, string errorMessage = null, Exception ex = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        Exception = ex;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static Result<T> Failure(string errorMessage, Exception ex = null)
        => new(false, default, errorMessage, ex);

    /// <summary>
    /// Tries an action and returns a Result.
    /// </summary>
    public static Result<T> Try(Func<T> action, string errorMessage = "An error occurred")
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        try
        {
            return Success(action());
        }
        catch (Exception ex)
        {
            return Failure(errorMessage, ex);
        }
    }

    /// <summary>
    /// Transforms the value if the result is successful.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        return IsFailure ? Result<TOut>.Failure(ErrorMessage, Exception) : Result<TOut>.Success(transform(Value));
    }

    /// <summary>
    /// Transforms the error message if the result is a failure.
    /// </summary>
    public Result<T> MapError(Func<string, string> transform)
    {
        if (transform == null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        return IsSuccess ? this : Failure(transform(ErrorMessage), Exception);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsSuccess)
        {
            action(Value);
        }

        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public Result<T> OnFailure(Action<string, Exception> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsFailure)
        {
            action(ErrorMessage, Exception);
        }

        return this;
    }

    /// <summary>
    /// Indicates if the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Combines multiple results. Returns the last successful value or the first failure.
    /// </summary>
    public static Result<T> Combine(params Result<T>[] results)
    {
        if (results == null)
        {
            throw new ArgumentNullException(nameof(results));
        }

        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                return result;
            }
        }

        var lastResult = results.LastOrDefault();

        if (lastResult == null || lastResult.IsFailure)
        {
            return Failure("All combined results were failures.");
        }

        return Success(lastResult.Value);
    }

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() => IsSuccess ? $"Success: {Value}" : $"Failure: {ErrorMessage}";
}

public class Result : Result<string>
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static new Result Success() => new(true);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static Result Failure(string errorMessage, Exception ex = null)
        => new(false, errorMessage: errorMessage, ex: ex);

    /// <summary>
    /// Implicit conversion from string to Result.
    /// </summary>
    public static implicit operator Result(string message) => new(true, value: message);

    /// <summary>
    /// Tries an action and returns a Result.
    /// </summary>
    public static Result Try(Action action, string errorMessage = "An error occurred")
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        try
        {
            action();
            return Success();
        }
        catch (Exception ex)
        {
            return Failure(errorMessage, ex);
        }
    }

    private Result(bool isSuccess, string value = null, string errorMessage = null, Exception ex = null)
        : base(isSuccess, value, errorMessage, ex) { }
}
