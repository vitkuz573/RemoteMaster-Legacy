// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Windows.Abstractions;

namespace RemoteMaster.Host.Windows.Hubs;

public class DeviceManagerHub(IDeviceManagerService deviceManagerService) : Hub<IDeviceManagerClient>
{
    public async Task GetDevices()
    {
        var devices = await Task.Run(deviceManagerService.GetDeviceList);

        await Clients.All.ReceiveDeviceList(devices);
    }

    public async Task DisableDevice(string deviceInstanceId)
    {
        var result = await Task.Run(() => deviceManagerService.DisableDeviceByInstanceId(deviceInstanceId));

        if (result)
        {
            await Clients.All.NotifyDeviceDisabled(deviceInstanceId);
        }
        else
        {
            await Clients.All.NotifyDeviceDisableFailed(deviceInstanceId);
        }
    }

    public async Task EnableDevice(string deviceInstanceId)
    {
        var result = await Task.Run(() => deviceManagerService.EnableDeviceByInstanceId(deviceInstanceId));

        if (result)
        {
            await Clients.All.NotifyDeviceEnabled(deviceInstanceId);
        }
        else
        {
            await Clients.All.NotifyDeviceEnableFailed(deviceInstanceId);
        }
    }
}
