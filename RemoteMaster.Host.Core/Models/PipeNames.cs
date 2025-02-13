// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Models;

public static class PipeNames
{
    public const string CommandPipe = "CommandPipe";
    public const string UpdaterReadyPipe = "UpdaterReadyPipe";
    public const string EnvironmentMonitorPipe = "EnvironmentMonitorPipe";
}
