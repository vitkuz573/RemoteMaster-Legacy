// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ServiceFactory : IServiceFactory
{
    private readonly Dictionary<string, Func<AbstractService>> _serviceCreators = [];

    public ServiceFactory()
    {
        LoadAllServices();
    }

    private void LoadAllServices()
    {
        var serviceTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(AbstractService).IsAssignableFrom(t));

        foreach (var type in serviceTypes)
        {
            if (Activator.CreateInstance(type) is AbstractService serviceInstance)
            {
                _serviceCreators[serviceInstance.Name] = () => (AbstractService)Activator.CreateInstance(type);
            }
        }
    }

    public AbstractService GetService(string serviceName)
    {
        if (_serviceCreators.TryGetValue(serviceName, out var constructor))
        {
            return constructor();
        }

        throw new ArgumentException($"Service for '{serviceName}' is not defined.");
    }
}