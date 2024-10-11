// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Server.Options;

public class WimBootOptions
{
    [JsonPropertyName("folderPath")]
    public string FolderPath { get; set; } = string.Empty;

    [JsonPropertyName("loaderCommand")]
    public string LoaderCommand { get; set; } = string.Empty;

    [JsonPropertyName("winFile")]
    public string WimFile { get; set; } = string.Empty;

    [JsonPropertyName("userName")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}
