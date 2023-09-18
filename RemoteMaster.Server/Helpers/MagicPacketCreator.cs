// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Helpers;

public static class MagicPacketCreator
{
    private const int MacAddressLength = 17;
    private const int MacAddressByteLength = 6;

    public static byte[] Create(string macAddress)
    {
        if (string.IsNullOrEmpty(macAddress))
        {
            throw new ArgumentNullException(nameof(macAddress));
        }

        if (macAddress.Length != MacAddressLength)
        {
            throw new ArgumentException("Invalid MAC address length. Expected 17 characters.");
        }

        var normalizedMac = NormalizeMacAddress(macAddress);
        var macBytes = Convert.FromHexString(normalizedMac.Replace(":", ""));

        if (macBytes.Length != MacAddressByteLength)
        {
            throw new ArgumentException("Invalid MAC address. Expected 6 segments.");
        }

        var packet = new byte[17 * MacAddressByteLength];

        for (var i = 0; i < MacAddressByteLength; i++)
        {
            packet[i] = 0xFF;
        }

        for (var i = 1; i <= 16; i++)
        {
            Buffer.BlockCopy(macBytes, 0, packet, i * MacAddressByteLength, MacAddressByteLength);
        }

        return packet;
    }

    private static string NormalizeMacAddress(string macAddress)
    {
        return macAddress.ToUpper().Replace('-', ':');
    }
}
