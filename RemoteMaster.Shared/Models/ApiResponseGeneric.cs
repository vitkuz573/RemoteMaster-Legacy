// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RemoteMaster.Shared.Models;

/// <summary>
/// Represents an API response with data.
/// </summary>
/// <typeparam name="TData">Type of the data payload.</typeparam>
public class ApiResponse<TData> : ApiResponseBase
{
    /// <summary>
    /// Data payload of the response.
    /// </summary>
    public TData? Data { get; }

    /// <summary>
    /// Hypermedia links to support HATEOAS.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, string>? Links { get; private set; }

    /// <summary>
    /// Constructor for a successful response with data.
    /// </summary>
    [JsonConstructor]
    public ApiResponse(TData data, string? message, int statusCode) : base(message, statusCode)
    {
        Data = data;
    }

    /// <summary>
    /// Constructor for a failure response with data.
    /// </summary>
    public ApiResponse(ProblemDetails error, int statusCode) : base(error, statusCode)
    {
    }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<TData> Success(TData data, string message = "Operation successful.", int statusCode = StatusCodes.Status200OK) => new(data, message, statusCode);

    /// <summary>
    /// Creates a failure response with data.
    /// </summary>
    public static ApiResponse<TData> Failure(ProblemDetails error, int statusCode = StatusCodes.Status400BadRequest) => new(error, statusCode);

    /// <summary>
    /// Sets hypermedia links for the API response.
    /// </summary>
    /// <param name="links">Dictionary of relation types and corresponding URIs.</param>
    public void SetLinks(Dictionary<string, string> links)
    {
        Links = links;
    }
}
