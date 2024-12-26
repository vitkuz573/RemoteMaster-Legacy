// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RemoteMaster.Shared.Models;

/// <summary>
/// Represents an API response without data.
/// </summary>
public class ApiResponse : ApiResponseBase
{
    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public ApiResponse() : base("Operation successful.", StatusCodes.Status200OK)
    {
    }

    /// <summary>
    /// Constructor for a successful response.
    /// </summary>
    public ApiResponse(string? message = "Operation successful.", int statusCode = StatusCodes.Status200OK) : base(message, statusCode)
    {
    }

    /// <summary>
    /// Constructor for a failure response.
    /// </summary>
    public ApiResponse(ProblemDetails error, int statusCode = StatusCodes.Status400BadRequest) : base(error, statusCode)
    {
    }

    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse Success(string message = "Operation successful.", int statusCode = StatusCodes.Status200OK) => new(message, statusCode);

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static ApiResponse Failure(ProblemDetails error, int statusCode = StatusCodes.Status400BadRequest) => new(error, statusCode);
}
