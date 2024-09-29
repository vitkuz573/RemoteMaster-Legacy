// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

public partial record SerialNumber
{
    private const int MaxSerialNumberByteSize = 20;
    private const int MinSerialNumberByteSize = 1;
    private const string HexadecimalPattern = "^[0-9A-F]+$";
    private const int FirstByteMask = 0x7F;
    private const string HexSeparator = "-";

    public string Value { get; }

    [GeneratedRegex(HexadecimalPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SerialNumberRegex();

    private SerialNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Serial number cannot be empty", nameof(value));
        }

        if (!SerialNumberRegex().IsMatch(value))
        {
            throw new ArgumentException("Serial number must contain only hexadecimal characters.");
        }

        if (value.Length / 2 < MinSerialNumberByteSize || value.Length / 2 > MaxSerialNumberByteSize)
        {
            throw new ArgumentException($"Serial number must be between {MinSerialNumberByteSize} and {MaxSerialNumberByteSize} bytes.");
        }

        Value = value;
    }

    public static SerialNumber FromExistingValue(string value)
    {
        return new SerialNumber(value);
    }

    public static SerialNumber Generate()
    {
        var randomBytes = GenerateSecureRandomBytes(MaxSerialNumberByteSize);
        randomBytes[0] &= FirstByteMask;

        var serialNumberString = BitConverter.ToString(randomBytes).Replace(HexSeparator, string.Empty);

        return new SerialNumber(serialNumberString);
    }

    public byte[] ToByteArray()
    {
        return Enumerable.Range(0, Value.Length / 2)
            .Select(x => Convert.ToByte(Value.Substring(x * 2, 2), 16))
            .ToArray();
    }

    private static byte[] GenerateSecureRandomBytes(int byteSize)
    {
        var randomBytes = new byte[byteSize];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return randomBytes;
    }
}
