// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;

namespace RemoteMaster.Server.ValueObjects;

public class SerialNumber
{
    private SerialNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Serial number cannot be empty", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static SerialNumber FromExistingValue(string value)
    {
        return new SerialNumber(value);
    }

    public static SerialNumber Generate()
    {
        var timestamp = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var uuid = Guid.NewGuid().ToByteArray();
        var randomBytes = new byte[16];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var combinedBytes = new byte[timestamp.Length + uuid.Length + randomBytes.Length];
        Array.Copy(timestamp, 0, combinedBytes, 0, timestamp.Length);
        Array.Copy(uuid, 0, combinedBytes, timestamp.Length, uuid.Length);
        Array.Copy(randomBytes, 0, combinedBytes, timestamp.Length + uuid.Length, randomBytes.Length);

        var hashedBytes = SHA3_256.IsSupported ? SHA3_256.HashData(combinedBytes) : SHA256.HashData(combinedBytes);
        var serialNumberString = BitConverter.ToString(hashedBytes).Replace("-", string.Empty);

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
}
