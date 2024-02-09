// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Models;

public class HostServiceConfiguration : IServiceConfiguration
{
    public string Name => "RCHost";

    public string DisplayName => "RemoteMaster Control Service";

    public string Description => "RemoteMaster Control Service enables advanced remote management and control functionalities for authorized clients. It provides seamless access to system controls, resource management, and real-time support capabilities, ensuring efficient and secure remote operations.";

    public string StartType => "auto";

    public IEnumerable<string>? Dependencies => null;
}
