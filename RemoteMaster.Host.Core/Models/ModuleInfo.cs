// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Host.Core.Models;

public class ModuleInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public Version Version { get; set; } = new Version(1, 0);

    [JsonPropertyName("releaseDate")]
    public DateTime? ReleaseDate { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("dependencies")]
    public string[] Dependencies { get; set; } = [];

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = string.Empty;
}
