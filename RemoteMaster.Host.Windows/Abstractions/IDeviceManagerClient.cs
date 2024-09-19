// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IDeviceManagerClient
{
    Task ReceiveDeviceList(List<DeviceDto> devices);

    Task NotifyDeviceDisabled(string deviceInstanceId);

    Task NotifyDeviceDisableFailed(string deviceInstanceId);

    Task NotifyDeviceEnabled(string deviceInstanceId);

    Task NotifyDeviceEnableFailed(string deviceInstanceId);
}
