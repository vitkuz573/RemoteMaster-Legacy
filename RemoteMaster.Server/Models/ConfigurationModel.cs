// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Models;

public class ConfigurationModel
{
    [JsonPropertyName("server")]
    public string Server { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; }
}