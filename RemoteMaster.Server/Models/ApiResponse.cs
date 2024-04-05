// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class ApiResponse(bool success, string message, object? data = null)
{
    public bool Success { get; set; } = success;

    public string Message { get; set; } = message;

    public object? Data { get; set; } = data;
}
