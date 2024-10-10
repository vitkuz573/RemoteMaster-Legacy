// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ServiceFactory : IServiceFactory
{
    private readonly Dictionary<string, AbstractService> _serviceInstances;

    public ServiceFactory(IEnumerable<AbstractService> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _serviceInstances = new Dictionary<string, AbstractService>();

        foreach (var service in services)
        {
            _serviceInstances[service.Name] = service;
        }
    }

    public AbstractService GetService(string serviceName)
    {
        if (_serviceInstances.TryGetValue(serviceName, out var serviceInstance))
        {
            return serviceInstance;
        }

        throw new ArgumentException($"Service for '{serviceName}' is not defined.");
    }
}
