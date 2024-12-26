// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;

namespace RemoteMaster.Shared.Models;

/// <summary>
/// Base class for API responses.
/// </summary>
public abstract class ApiResponseBase
{
    /// <summary>
    /// Status code of the response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Message describing the outcome of the operation.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Indicates whether the response represents a successful outcome.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Standardized error format for failure scenarios.
    /// </summary>
    public ProblemDetails? Error { get; }

    /// <summary>
    /// Constructor for a successful response.
    /// </summary>
    protected ApiResponseBase(string? message, int statusCode)
    {
        Message = message;
        StatusCode = statusCode;
        IsSuccess = true;
    }

    /// <summary>
    /// Constructor for a failure response.
    /// </summary>
    protected ApiResponseBase(ProblemDetails error, int statusCode)
    {
        Error = error;
        StatusCode = statusCode;
        IsSuccess = false;
        Message = null;
    }
}
