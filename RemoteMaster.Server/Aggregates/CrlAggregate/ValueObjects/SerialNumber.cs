// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

public class SerialNumber
{
    private const int MaxSerialNumberByteSize = 20;
    private const int MinSerialNumberByteSize = 1;

    private static readonly Regex SerialNumberRegex = new("^[0-9A-F]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Value { get; }

    private SerialNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Serial number cannot be empty", nameof(value));
        }

        if (!SerialNumberRegex.IsMatch(value))
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

        randomBytes[0] &= 0x7F;

        var serialNumberString = BitConverter.ToString(randomBytes).Replace("-", string.Empty);

        serialNumberString = TrimLeadingZeros(serialNumberString);

        return new SerialNumber(serialNumberString);
    }

    public byte[] ToByteArray()
    {
        return Enumerable.Range(0, Value.Length / 2)
            .Select(x => Convert.ToByte(Value.Substring(x * 2, 2), 16))
            .ToArray();
    }

    public override bool Equals(object? obj) => obj is SerialNumber other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode();

    private static byte[] GenerateSecureRandomBytes(int byteSize)
    {
        var randomBytes = new byte[byteSize];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return randomBytes;
    }

    private static string TrimLeadingZeros(string hex)
    {
        return hex.TrimStart('0');
    }
}
