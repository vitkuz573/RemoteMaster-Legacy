// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class Result
{
    public bool IsSuccess { get; }

    public List<ErrorDetails> Errors { get; }

    protected Result(bool isSuccess, List<ErrorDetails>? errors)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true, null);

    public static Result Failure(params ErrorDetails[] errors) => new(false, new List<ErrorDetails>(errors));

    public static Result Failure(string message, string? code = null, Exception? exception = null) => new(false, [new(message, code, exception)]);

    public Result AddError(string message, string? code = null, Exception? exception = null)
    {
        if (IsSuccess)
        {
            throw new InvalidOperationException("Cannot add errors to a successful result.");
        }

        Errors.Add(new ErrorDetails(message, code, exception));

        return this;
    }
}