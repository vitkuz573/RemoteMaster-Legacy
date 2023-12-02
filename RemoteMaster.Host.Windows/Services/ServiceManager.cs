// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ServiceManager : IServiceManager
{
    public bool IsServiceInstalled(string serviceName) => ServiceController.GetServices().Any(service => service.ServiceName == serviceName);

    public void InstallService(IServiceConfiguration serviceConfig, string executablePath)
    {
        ArgumentNullException.ThrowIfNull(serviceConfig);

        ExecuteServiceCommand($"create {serviceConfig.Name} DisplayName= \"{serviceConfig.DisplayName}\" binPath= \"{executablePath}\" start= {serviceConfig.StartType}");

        if (!string.IsNullOrWhiteSpace(serviceConfig.Description))
        {
            ExecuteServiceCommand($"description {serviceConfig.Name} \"{serviceConfig.Description}\"");
        }

        if (serviceConfig.Dependencies != null && serviceConfig.Dependencies.Any())
        {
            var dependenciesStr = string.Join("/", serviceConfig.Dependencies);
            ExecuteServiceCommand($"config {serviceConfig.Name} depend= {dependenciesStr}");
        }
    }

    public void StartService(string serviceName)
    {
        using var serviceController = new ServiceController(serviceName);

        if (serviceController.Status != ServiceControllerStatus.Running)
        {
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running);
        }
    }

    public void StopService(string serviceName)
    {
        using var serviceController = new ServiceController(serviceName);

        if (serviceController.Status != ServiceControllerStatus.Stopped)
        {
            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
        }
    }

    public void UninstallService(string serviceName) => ExecuteServiceCommand($"delete {serviceName}");

    private static void ExecuteServiceCommand(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "sc",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            }
        };

        process.Start();
        process.WaitForExit();
    }
}
