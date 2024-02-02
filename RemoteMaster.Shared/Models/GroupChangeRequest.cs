// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public class GroupChangeRequest(string macAddress, string newGroup)
{
    public string MACAddress { get; set; } = macAddress;

    public string NewGroup { get; set; } = newGroup;
}
