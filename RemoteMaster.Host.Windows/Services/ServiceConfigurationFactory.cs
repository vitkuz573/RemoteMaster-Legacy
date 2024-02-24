// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class ServiceConfigurationFactory : IServiceConfigurationFactory
{
    public AbstractService GetServiceConfiguration(string serviceName)
    {
        return serviceName switch
        {
            "RCHost" => new HostAbstractService(),
            "RCUpdater" => new UpdaterAbstractService(),
            _ => throw new ArgumentException($"Service configuration for '{serviceName}' is not defined.")
        };
    }
}