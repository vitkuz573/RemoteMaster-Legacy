// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T? value, bool isSuccess, List<ErrorDetails>? errors) : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, null);

    public static new Result<T> Failure(params ErrorDetails[] errors) => new(default, false, new List<ErrorDetails>(errors));

    public static new Result<T> Failure(string message, string? code = null, Exception? exception = null) => new(default, false, new List<ErrorDetails> { new ErrorDetails(message, code, exception) });

    public new void AddError(string message, string? code = null, Exception? exception = null)
    {
        Errors.Add(new ErrorDetails(message, code, exception));
    }
}
