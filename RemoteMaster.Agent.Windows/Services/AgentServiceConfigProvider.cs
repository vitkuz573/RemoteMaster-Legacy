// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Services;

public class AgentServiceConfigProvider
{
    public string ServiceName { get; } = "RCService";

    public string ServiceDisplayName { get; } = "Remote Control Service";

    public string ServiceStartType { get; } = "delayed-auto";

    public IEnumerable<string>? ServiceDependencies { get; } = new[] { "LanmanWorkstation" };
}
