﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Enums;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Hubs;

public class DeviceManagerHub(IDeviceManagerService deviceManagerService) : Hub<IDeviceManagerClient>
{
    [Authorize(Policy = "ViewDevicesPolicy")]
    [HubMethodName("GetDevices")]
    public async Task GetDevicesAsync()
    {
        var devices = deviceManagerService.GetDevices();

        await Clients.Caller.ReceiveDeviceList(devices);
    }

    [Authorize(Policy = "DisableDevicePolicy")]
    [HubMethodName("DisableDevice")]
    public async Task DisableDeviceAsync(string deviceInstanceId)
    {
        var result = deviceManagerService.DisableDeviceByInstanceId(deviceInstanceId);

        if (result)
        {
            await Clients.Caller.NotifyDeviceStatusChanged(deviceInstanceId, "Device disabled successfully", NotificationSeverity.Success);
        }
        else
        {
            await Clients.Caller.NotifyDeviceStatusChanged(deviceInstanceId, "Failed to disable device", NotificationSeverity.Error);
        }
    }

    [Authorize(Policy = "EnableDevicePolicy")]
    [HubMethodName("EnableDevice")]
    public async Task EnableDeviceAsync(string deviceInstanceId)
    {
        var result = deviceManagerService.EnableDeviceByInstanceId(deviceInstanceId);

        if (result)
        {
            await Clients.Caller.NotifyDeviceStatusChanged(deviceInstanceId, "Device enabled successfully", NotificationSeverity.Success);
        }
        else
        {
            await Clients.Caller.NotifyDeviceStatusChanged(deviceInstanceId, "Failed to enable device", NotificationSeverity.Error);
        }
    }

    [Authorize(Policy = "UpdateDeviceDriverPolicy")]
    public void UpdateDeviceDriver(DriverUpdateRequest driveUpdateRequest)
    {
        ArgumentNullException.ThrowIfNull(driveUpdateRequest);

        deviceManagerService.UpdateDeviceDriver(driveUpdateRequest.HardwareId, driveUpdateRequest.InfFilePath);
    }
}
