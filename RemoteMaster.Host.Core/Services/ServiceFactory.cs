// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ServiceFactory : IServiceFactory
{
    private readonly Dictionary<string, IService> _serviceInstances;

    public ServiceFactory(IEnumerable<IService> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _serviceInstances = [];

        foreach (var service in services)
        {
            _serviceInstances[service.Name] = service;
        }
    }

    public IService GetService(string serviceName)
    {
        if (_serviceInstances.TryGetValue(serviceName, out var serviceInstance))
        {
            return serviceInstance;
        }

        throw new ArgumentException($"Service for '{serviceName}' is not defined.");
    }
}
