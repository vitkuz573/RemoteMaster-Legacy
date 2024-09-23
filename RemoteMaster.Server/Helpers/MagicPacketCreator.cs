// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Helpers;

public static class MagicPacketCreator
{
    public static byte[] Create(PhysicalAddress macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        var macBytes = macAddress.GetAddressBytes();

        return Enumerable.Repeat((byte)0xFF, 6)
                         .Concat(Enumerable.Repeat(macBytes, 16).SelectMany(b => b))
                         .ToArray();
    }
}
