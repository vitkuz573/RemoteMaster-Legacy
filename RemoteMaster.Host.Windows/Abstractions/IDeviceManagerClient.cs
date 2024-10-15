// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Enums;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Windows.Abstractions;

public interface IDeviceManagerClient
{
    Task ReceiveDeviceList(List<DeviceDto> devices);

    Task NotifyDeviceStatusChanged(string deviceInstanceId, string message, NotificationSeverity severity);
}
