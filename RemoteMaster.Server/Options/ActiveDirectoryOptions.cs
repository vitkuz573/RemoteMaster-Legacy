// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Options;

public class ActiveDirectoryOptions
{
    [JsonPropertyName("method")]
    public ActiveDirectoryMethod Method { get; set; }

    [JsonPropertyName("server")]
    public string Server { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; } = 389;

    [JsonPropertyName("searchBase")]
    public string SearchBase { get; set; } = string.Empty;

    [JsonPropertyName("templateName")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
