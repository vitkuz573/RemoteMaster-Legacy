// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.ServiceProcess;
using RemoteMaster.Agent.Abstractions;

namespace RemoteMaster.Agent.Services;

public class ServiceManager : IServiceManager
{
    private const string ServiceName = "RCService";
    private const string ServiceDisplayName = "Remote Control Service";

    public bool IsServiceInstalled()
    {
        return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
    }

    public void InstallService(string executablePath)
    {
        ExecuteServiceCommand($"create {ServiceName} DisplayName= \"{ServiceDisplayName}\" binPath= \"{executablePath}\" start= auto");
        ExecuteServiceCommand($"config {ServiceName} depend= LanmanWorkstation");
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

    public void UninstallService()
    {
        ExecuteServiceCommand($"delete {ServiceName}");
    }

    private static void ExecuteServiceCommand(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "sc",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Verb = "runas"
        };

        using var process = new Process { StartInfo = processStartInfo };

        process.Start();
        process.WaitForExit();
    }
}
