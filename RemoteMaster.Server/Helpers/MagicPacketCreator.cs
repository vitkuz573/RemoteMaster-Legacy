// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Helpers;

public static class MagicPacketCreator
{
    public static byte[] Create(string macAddress)
    {
        if (macAddress == null)
        {
            throw new ArgumentNullException(nameof(macAddress));
        }

        var macBytes = ParseMacAddress(macAddress);
        var packet = new byte[17 * 6];

        for (var i = 0; i < 6; i++)
        {
            packet[i] = 0xFF;
        }

        for (var i = 1; i <= 16; i++)
        {
            for (var j = 0; j < 6; j++)
            {
                packet[i * 6 + j] = macBytes[j];
            }
        }

        return packet;
    }

    private static byte[] ParseMacAddress(string macAddress)
    {
        if (macAddress.Length != 17)
        {
            throw new ArgumentException("Invalid MAC address length. Expected 17 characters.");
        }

        var colonCount = 0;
        var dashCount = 0;

        foreach (var c in macAddress)
        {
            if (c == ':')
            {
                colonCount++;
            }
            else if (c == '-')
            {
                dashCount++;
            }
        }

        if ((colonCount > 0 && dashCount > 0) || (colonCount + dashCount != 5))
        {
            throw new ArgumentException("Invalid MAC address. Unexpected delimiter characters found.");
        }

        var hexValues = macAddress.Split(':', '-');

        if (hexValues.Length != 6)
        {
            throw new ArgumentException("Invalid MAC address. Expected 6 segments.");
        }

        var macBytes = new byte[6];

        for (var i = 0; i < hexValues.Length; i++)
        {
            if (hexValues[i].Length != 2 || !IsHexadecimal(hexValues[i]))
            {
                throw new ArgumentException($"Invalid MAC address segment at position {i + 1}: '{hexValues[i]}'.");
            }

            macBytes[i] = Convert.ToByte(hexValues[i], 16);
        }

        return macBytes;
    }

    private static bool IsHexadecimal(string value)
    {
        const string allowedChars = "0123456789ABCDEFabcdef";

        foreach (var ch in value)
        {
            if (!allowedChars.Contains(ch))
            {
                return false;
            }
        }

        return true;
    }
}
