// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Models;

/// <summary>
/// Represents a generic API response with a status code, message, and optional data payload.
/// </summary>
public record ApiResponse<TData>(int StatusCode, string Message, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TData? Data)
{
    /// <summary>
    /// Factory method to create a success response.
    /// </summary>
    public static ApiResponse<TData> Success(TData data, string message = "Operation successful.")
        => new(StatusCodes.Status200OK, message, data);

    /// <summary>
    /// Factory method to create a failure response.
    /// </summary>
    public static ApiResponse<T> Failure<T>(string message, int statusCode = StatusCodes.Status400BadRequest)
        => new(statusCode, message, default);

    /// <summary>
    /// Optional links for HATEOAS support.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, string>? Links { get; init; }
}

