// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ServiceProcess;
using RemoteMaster.Updater.Abstractions;

namespace RemoteMaster.Updater.Services;

public class ServiceManager : IServiceManager
{
    private const string ServiceName = "RCService";

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
}
