// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class HealthCheck(string name, string status, int statusCode, string duration, string? description, string? exception, Dictionary<string, string?> data)
{
    public string Name { get; set; } = name;

    public string Status { get; set; } = status;

    public int StatusCode { get; set; } = statusCode;

    public string Duration { get; set; } = duration;

    public string? Description { get; set; } = description;

    public string? Exception { get; set; } = exception;

    public Dictionary<string, string?> Data { get; } = new(data);
}

