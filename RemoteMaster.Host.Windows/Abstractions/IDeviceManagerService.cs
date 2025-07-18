﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IDeviceManagerService
{
    IEnumerable<DeviceDto> GetDevices();

    bool DisableDeviceByInstanceId(string deviceInstanceId);

    bool EnableDeviceByInstanceId(string deviceInstanceId);

    bool StopDeviceByInstanceId(string deviceInstanceId);

    bool StartDeviceByInstanceId(string deviceInstanceId);

    bool UpdateDeviceDriver(string hardwareId, string infFilePath);
}
