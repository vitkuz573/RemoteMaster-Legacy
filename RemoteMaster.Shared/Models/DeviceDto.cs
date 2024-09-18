// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class DeviceDto(Guid id, string name, string deviceClass, string manufacturer, string hardwareId, string compatibleIds, string locationInfo, string service, string classGuid, bool isEnabled)
{
    public Guid Id { get; set; } = id;

    public string Name { get; set; } = name;

    public string DeviceClass { get; set; } = deviceClass;

    public string Manufacturer { get; set; } = manufacturer;

    public string HardwareId { get; set; } = hardwareId;

    public string CompatibleIds { get; set; } = compatibleIds;

    public string LocationInfo { get; set; } = locationInfo;

    public string Service { get; set; } = service;

    public string ClassGuid { get; set; } = classGuid;

    public bool IsEnabled { get; set; } = isEnabled;
}
