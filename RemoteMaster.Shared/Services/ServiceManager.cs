// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Shared.Services;

public class ServiceManager : IServiceManager
{
    private const string ServiceName = "RCService";
    private const string ServiceDisplayName = "Remote Control Service";
    private const string ServiceStartType = "delayed-auto";
    private static readonly string[] ServiceDependencies = { "LanmanWorkstation" };

    public bool IsServiceInstalled() => ServiceController.GetServices().Any(service => service.ServiceName == ServiceName);

    public void InstallService(string executablePath)
    {
        ExecuteServiceCommand($"create {ServiceName} DisplayName= \"{ServiceDisplayName}\" binPath= \"{executablePath}\" start= {ServiceStartType}");

        if (ServiceDependencies.Any())
        {
            var dependencies = string.Join("/", ServiceDependencies);
            ExecuteServiceCommand($"config {ServiceName} depend= {dependencies}");
        }
    }

    public void StartService()
    {
        using var serviceController = new ServiceController(ServiceName);

        if (serviceController.Status != ServiceControllerStatus.Running)
        {
            serviceController.Start();
            serviceController.WaitForStatus(ServiceControllerStatus.Running);
        }
    }

    public void StopService()
    {
        using var serviceController = new ServiceController(ServiceName);

        if (serviceController.Status != ServiceControllerStatus.Stopped)
        {
            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
        }
    }

    public void UninstallService() => ExecuteServiceCommand($"delete {ServiceName}");

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
