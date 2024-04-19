// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Models;

public class ApiResponse<T>(bool success, string message, T? data = default)
{
    public bool Success { get; set; } = success;

    public string Message { get; set; } = message;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; } = data;
}
