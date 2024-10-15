// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.DTOs;

public class DriverUpdateRequest(string hardwareId, string infFilePath)
{
    public string HardwareId { get; } = hardwareId;

    public string InfFilePath { get; } = infFilePath;
}
