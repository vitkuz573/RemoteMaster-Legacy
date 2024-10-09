// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class DeviceDto(string name, string deviceClass, string manufacturer, string hardwareId, string compatibleIds, string locationInfo, string service, string deviceInstanceId, bool isEnabled)
{
    public string Name { get; } = name;

    public string DeviceClass { get; } = deviceClass;

    public string Manufacturer { get; } = manufacturer;

    public string HardwareId { get; } = hardwareId;

    public string CompatibleIds { get; } = compatibleIds;

    public string LocationInfo { get; } = locationInfo;

    public string Service { get; } = service;

    public string DeviceInstanceId { get; } = deviceInstanceId;

    public bool IsEnabled { get; set; } = isEnabled;
}
