// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Shared.Models;

/// <summary>
/// Represents a uniform API response with a status code, message, and optional data payload.
/// Enhances client-server communication by ensuring consistency, predictability, and simplicity.
/// </summary>
public record ApiResponse<TData>(int StatusCode, string Message, TData Data)
{
    /// <summary>
    /// Factory method to create a success response with standard message and status code.
    /// </summary>
    public static ApiResponse<TData> Success(TData data, string message = "Operation successful.")
        => new(StatusCodes.Status200OK, message, data);

    /// <summary>
    /// Factory method to create a failure response with a custom message and status code.
    /// </summary>
    public static ApiResponse<T> Failure<T>(string message, int statusCode = StatusCodes.Status400BadRequest)
        => new(statusCode, message, default!);  // Используем default! для значения по умолчанию

    /// <summary>
    /// Hypermedia links to support HATEOAS, allowing clients to navigate related resources dynamically.
    /// </summary>
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Links { get; private set; }

    /// <summary>
    /// Sets hypermedia links for the API response, adhering to HATEOAS principles.
    /// </summary>
    /// <param name="links">Dictionary of relation types and corresponding URIs.</param>
    public void SetLinks(Dictionary<string, string> links)
    {
        Links = links;
    }

    /// <summary>
    /// Includes a standardized error format for failure scenarios, facilitating error handling in client applications.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Error { get; private set; }

    /// <summary>
    /// Adds an error object to the response, used primarily for failure responses to provide additional error details.
    /// </summary>
    /// <param name="error">The error object containing error details.</param>
    public void SetError(object error)
    {
        Error = error;
    }
}
