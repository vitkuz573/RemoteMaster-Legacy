// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Models;

public class ApiResponse<TData>(bool success, string message, TData? data = default)
{
    [JsonPropertyName("success")]
    public bool Success { get; init; } = success;

    [JsonPropertyName("message")]
    public string Message { get; init; } = message;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("data")]
    public TData? Data { get; init; } = data;
}
