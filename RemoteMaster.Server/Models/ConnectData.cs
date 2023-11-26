// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class ConnectData
{
    public List<Display> Displays { get; } = [];

    public int ImageQuality { get; set; }

    public bool CursorTracking { get; set; }

    public bool InputEnabled { get; set; }

    public string Version { get; set; }
}
