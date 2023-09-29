// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Agent.Models;

public static class UpdaterServiceConfig
{
    public static string ServiceName { get; } = "RCSUpdater";

    public static string ServiceDisplayName { get; } = "Remote Control Updater";

    public static string ServiceStartType { get; } = "auto";

    public static IEnumerable<string>? ServiceDependencies { get; } = null;
}
