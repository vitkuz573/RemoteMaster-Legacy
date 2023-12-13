// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IHostServiceManager
{
    Task InstallOrUpdate(HostConfiguration configuration, string hostName, string ipv4Address, string macAddress);

    Task Uninstall(HostConfiguration configuration, string hostName);

    Task UpdateHostInformation(HostConfiguration configuration, string hostname, string ipAddress, string macAddress);
}
