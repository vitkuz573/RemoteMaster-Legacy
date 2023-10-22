// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public class HostServiceConfiguration : IServiceConfiguration
{
    public string Name { get; } = "RCHost";

    public string DisplayName { get; } = "Remote Control Host";

    public string? Description => "Provides remote control and management capabilities for authorized clients. This service allows remote access to system functionalities and resources.";

    public string StartType { get; } = "auto";

    public IEnumerable<string>? Dependencies { get; } = null;
}
