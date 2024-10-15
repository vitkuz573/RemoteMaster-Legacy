// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Hubs;

public class DeviceManagerHub(IDeviceManagerService deviceManagerService) : Hub<IDeviceManagerClient>
{
    public async Task GetDevices()
    {
        var devices = deviceManagerService.GetDeviceList();

        await Clients.Caller.ReceiveDeviceList(devices);
    }

    public async Task DisableDevice(string deviceInstanceId)
    {
        var result = deviceManagerService.DisableDeviceByInstanceId(deviceInstanceId);

        if (result)
        {
            await Clients.Caller.NotifyDeviceDisabled(deviceInstanceId);
        }
        else
        {
            await Clients.Caller.NotifyDeviceDisableFailed(deviceInstanceId);
        }
    }

    public async Task EnableDevice(string deviceInstanceId)
    {
        var result = deviceManagerService.EnableDeviceByInstanceId(deviceInstanceId);

        if (result)
        {
            await Clients.Caller.NotifyDeviceEnabled(deviceInstanceId);
        }
        else
        {
            await Clients.Caller.NotifyDeviceEnableFailed(deviceInstanceId);
        }
    }

    public void UpdateDeviceDriver(DriverUpdateRequest driveUpdateRequest)
    {
        ArgumentNullException.ThrowIfNull(driveUpdateRequest);

        deviceManagerService.UpdateDeviceDriver(driveUpdateRequest.HardwareId, driveUpdateRequest.InfFilePath);
    }
}
