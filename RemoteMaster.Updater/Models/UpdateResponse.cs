// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json.Serialization;

namespace RemoteMaster.Updater.Models;

public class UpdateResponse
{
    public string ComponentName { get; set; }

    public Version CurrentVersion { get; set; }

    public Version AvailableVersion { get; set; }

    public bool IsUpdateAvailable { get; set; }

    public string Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ErrorResponse Error { get; set; }
}
