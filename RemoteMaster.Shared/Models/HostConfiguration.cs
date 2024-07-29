// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Shared.Models;

public class HostConfiguration
{
    [JsonPropertyName("server")]
    public string? Server { get; set; }

    [JsonPropertyName("subject")]
    public SubjectOptions Subject { get; set; }

    [JsonPropertyName("host")]
    public ComputerDto? Host { get; set; }
}