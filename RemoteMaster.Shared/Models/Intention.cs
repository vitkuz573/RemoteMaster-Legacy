// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Shared.Models;

public enum Intention
{
    /// <summary>
    /// Used to receive a thumbnail image from the remote device.
    /// </summary>
    ReceiveThumbnail,

    /// <summary>
    /// Used for general management and control operations on the remote device.
    /// </summary>
    ManageDevice
}

