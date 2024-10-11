// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RemoteMaster.Shared.Models;

/// <summary>
/// Represents a uniform API response with a status code and message.
/// Enhances client-server communication by ensuring consistency, predictability, and simplicity.
/// </summary>
public class ApiResponse
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
    /// Includes a standardized error format for failure scenarios, facilitating error handling in client applications.
    /// </summary>
    public ProblemDetails? Error { get; }

    /// <summary>
    /// Parameterless constructor for deserialization
    /// </summary>
    public ApiResponse()
    {
    }

    /// <summary>
    /// Constructor for success response.
    /// </summary>
    public ApiResponse(string? message = "Operation successful.", int statusCode = StatusCodes.Status200OK)
    {
        Message = message;
        StatusCode = statusCode;
        IsSuccess = true;
    }

    /// <summary>
    /// Constructor for failure response.
    /// </summary>
    public ApiResponse(ProblemDetails error, int statusCode = StatusCodes.Status400BadRequest)
    {
        Error = error;
        StatusCode = statusCode;
        IsSuccess = false;
        Message = null;
    }

    /// <summary>
    /// Creates a success response.
    /// </summary>
    public static ApiResponse Success(string message = "Operation successful.", int statusCode = StatusCodes.Status200OK) => new(message, statusCode);

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static ApiResponse Failure(ProblemDetails error, int statusCode = StatusCodes.Status400BadRequest) => new(error, statusCode);
}
