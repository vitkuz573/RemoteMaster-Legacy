// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Agent.Models;

public static class AgentServiceConfig
{
    public static string ServiceName { get; } = "RCService";

    public static string ServiceDisplayName { get; } = "Remote Control Service";

    public static string ServiceStartType { get; } = "delayed-auto";

    public static IEnumerable<string>? ServiceDependencies { get; } = new[] { "LanmanWorkstation" };
}
