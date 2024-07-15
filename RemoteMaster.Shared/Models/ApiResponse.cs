// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RemoteMaster.Shared.Models;

/// <summary>
/// Represents a uniform API response with a status code, message, and optional data payload.
/// Enhances client-server communication by ensuring consistency, predictability, and simplicity.
/// </summary>
public class ApiResponse<TData>
{
    /// <summary>
    /// Status code of the response.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Message describing the outcome of the operation.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Data payload of the response.
    /// </summary>
    public TData? Data { get; }

    /// <summary>
    /// Hypermedia links to support HATEOAS, allowing clients to navigate related resources dynamically.
    /// </summary>
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Links { get; private set; }

    /// <summary>
    /// Includes a standardized error format for failure scenarios, facilitating error handling in client applications.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProblemDetails? Error { get; private set; }

    /// <summary>
    /// Parameterless constructor for deserialization.
    /// </summary>
    public ApiResponse()
    {
    }

    /// <summary>
    /// Constructor for success response.
    /// </summary>
    [JsonConstructor]
    public ApiResponse(TData data, string message, int statusCode)
    {
        Data = data;
        Message = message;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Creates a success response.
    /// </summary>
    public static ApiResponse<TData> Success(TData data, string message = "Operation successful.", int statusCode = StatusCodes.Status200OK)
        => new(data, message, statusCode);

    /// <summary>
    /// Creates a failure response.
    /// </summary>
    public static ApiResponse<T> Failure<T>(string message, int statusCode = StatusCodes.Status400BadRequest)
        => new(default!, message, statusCode);

    /// <summary>
    /// Sets hypermedia links for the API response, adhering to HATEOAS principles.
    /// </summary>
    /// <param name="links">Dictionary of relation types and corresponding URIs.</param>
    public void SetLinks(Dictionary<string, string> links)
    {
        Links = links;
    }

    /// <summary>
    /// Adds an error object to the response, used primarily for failure responses to provide additional error details.
    /// </summary>
    /// <param name="error">The error object containing error details.</param>
    public void SetError(ProblemDetails error)
    {
        Error = error;
    }
}