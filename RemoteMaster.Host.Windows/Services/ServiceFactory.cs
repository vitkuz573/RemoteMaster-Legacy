// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class ServiceFactory : IServiceFactory
{
    public AbstractService GetService(string serviceName)
    {
        return serviceName switch
        {
            "RCHost" => new HostService(),
            "RCUpdater" => new UpdaterService(),
            _ => throw new ArgumentException($"Service configuration for '{serviceName}' is not defined.")
        };
    }
}