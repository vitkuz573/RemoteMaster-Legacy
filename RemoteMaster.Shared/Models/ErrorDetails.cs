// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class ErrorDetails(string message, string? code = null, Exception? exception = null)
{
    public string Message { get; } = message;

    public string? Code { get; } = code;

    public Exception? Exception { get; } = exception;

    public override string ToString() => $"Code: {Code}, Message: {Message}, Exception: {Exception?.Message}";
}
