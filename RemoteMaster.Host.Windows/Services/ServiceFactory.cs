// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ServiceFactory : IServiceFactory
{
    private readonly Dictionary<string, AbstractService> _serviceInstances;

    public ServiceFactory() : this(null) { }

    public ServiceFactory(IEnumerable<AbstractService> services)
    {
        _serviceInstances = [];

        if (services == null)
        {
            LoadAllServices();
        }
        else
        {
            foreach (var service in services)
            {
                _serviceInstances[service.Name] = service;
            }
        }
    }

    private void LoadAllServices()
    {
        var serviceTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AbstractService).IsAssignableFrom(t));

        foreach (var type in serviceTypes)
        {
            if (Activator.CreateInstance(type) is AbstractService serviceInstance)
            {
                _serviceInstances[serviceInstance.Name] = serviceInstance;
            }
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