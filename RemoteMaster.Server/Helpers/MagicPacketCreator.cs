// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;

namespace RemoteMaster.Server.Helpers;

public static class MagicPacketCreator
{
    private const int MacAddressByteLength = 6;

    public static byte[] Create(PhysicalAddress macAddress)
    {
        ArgumentNullException.ThrowIfNull(macAddress);

        var packet = new byte[17 * MacAddressByteLength];

        for (var i = 0; i < MacAddressByteLength; i++)
        {
            packet[i] = 0xFF;
        }

        for (var i = 1; i <= 16; i++)
        {
            Buffer.BlockCopy(macAddress.GetAddressBytes(), 0, packet, i * MacAddressByteLength, MacAddressByteLength);
        }

        return packet;
    }
}