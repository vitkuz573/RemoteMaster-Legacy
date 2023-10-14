// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Host.Models;

public class HostServiceConfig : IServiceConfig
{
    public string Name { get; } = "RCService";

    public string DisplayName { get; } = "Remote Control Service";

    public string StartType { get; } = "auto";

    public IEnumerable<string>? Dependencies { get; } = new[] { "LanmanWorkstation" };
}
