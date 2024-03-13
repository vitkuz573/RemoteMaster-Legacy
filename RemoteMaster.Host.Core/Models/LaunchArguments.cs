// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Models;

public class LaunchArguments
{
    public LaunchMode LaunchMode { get; set; } = LaunchMode.Default;

    public bool HelpRequested { get; init; }

    public string FolderPath { get; set; } = string.Empty;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool ForceUpdate { get; set; }
}
