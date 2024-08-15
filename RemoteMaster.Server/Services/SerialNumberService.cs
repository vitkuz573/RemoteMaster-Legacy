// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using FluentResults;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class SerialNumberService : ISerialNumberService
{
    /// <inheritdoc />
    public Result<byte[]> GenerateSerialNumber()
    {
        try
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

            return Result.Ok(hashedBytes);
        }
        catch (Exception ex)
        {
            return Result.Fail<byte[]>("Failed to generate serial number.").WithError(ex.Message);
        }
    }
}
