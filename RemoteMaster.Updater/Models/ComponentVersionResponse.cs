// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Updater.Models;

public class ComponentVersionResponse
{
    public string ComponentName { get; set; }

    public Version CurrentVersion { get; set; }
}
