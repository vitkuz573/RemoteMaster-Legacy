// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class Result<T> : Result
{
    public T Value { get; }

    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(List<ErrorDetails> errors) : base(false, errors)
    {
        Value = default!;
    }

    public static Result<T> Success(T value) => new(value);

    public static new Result<T> Failure(params ErrorDetails[] errors) => new(new List<ErrorDetails>(errors));

    public static new Result<T> Failure(string message, string? code = null, Exception? exception = null) => new(new List<ErrorDetails> { new ErrorDetails(message, code, exception) });

    public new Result<T> AddError(string message, string? code = null, Exception? exception = null)
    {
        if (IsSuccess)
        {
            throw new InvalidOperationException("Cannot add errors to a successful result.");
        }

        Errors.Add(new ErrorDetails(message, code, exception));

        return this;
    }
}