// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class HealthCheck
{
    public string Name { get; set; }

    public string Status { get; set; }

    public int StatusCode { get; set; }

    public string Duration { get; set; }

    public string Description { get; set; }

    public string? Exception { get; set; }

#pragma warning disable CA2227
    public Dictionary<string, string> Data { get; set; }
#pragma warning restore CA2227
}
