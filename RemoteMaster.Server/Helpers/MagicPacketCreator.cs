namespace RemoteMaster.Server.Helpers;

public static class MagicPacketCreator
{
    private const int MacAddressByteLength = 6;

    public static byte[] Create(string macAddress)
    {
        ArgumentException.ThrowIfNullOrEmpty(macAddress);

        var cleanMac = RemoveNonHexCharacters(macAddress);

        if (cleanMac.Length != MacAddressByteLength * 2)
        {
            throw new ArgumentException($"Invalid MAC address length. Expected 12 hex characters, got {cleanMac.Length}.");
        }

        var macBytes = Convert.FromHexString(cleanMac);

        if (macBytes.Length != MacAddressByteLength)
        {
            throw new ArgumentException("Invalid MAC address. Expected 6 bytes after conversion.");
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

    private static string RemoveNonHexCharacters(string input)
    {
        return string.Concat(input.Where(c => c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f'));
    }
}