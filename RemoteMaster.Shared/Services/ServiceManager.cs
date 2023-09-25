// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Services;

public class ServiceManager : IServiceManager
{
    public bool IsServiceInstalled(string serviceName) => ServiceController.GetServices().Any(service => service.ServiceName == serviceName);

    public void InstallService(string serviceName, string displayName, string executablePath, string startType, IEnumerable<string> dependencies)
    {
        ExecuteServiceCommand($"create {serviceName} DisplayName= \"{displayName}\" binPath= \"{executablePath}\" start= {startType}");

        if (dependencies != null && dependencies.Any())
        {
            var dependenciesStr = string.Join("/", dependencies);
            ExecuteServiceCommand($"config {serviceName} depend= {dependenciesStr}");
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
