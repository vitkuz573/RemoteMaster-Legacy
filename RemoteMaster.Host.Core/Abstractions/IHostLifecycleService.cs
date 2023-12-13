// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IHostLifecycleService
{
    Task RegisterAsync(HostConfiguration config, string hostName, string ipAddress, string macAddress);

    Task UnregisterAsync(HostConfiguration config, string hostName);

    Task UpdateHostInformationAsync(HostConfiguration config, string hostname, string ipAddress, string macAddress);

    Task<bool> IsHostRegisteredAsync(HostConfiguration config, string hostName);
}
