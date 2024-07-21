// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class SerialNumberService : ISerialNumberService
{
    /// <inheritdoc />
    public Result<byte[]> GenerateSerialNumber()
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var uuid = Guid.NewGuid().ToByteArray();
            var randomBytes = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var combinedBytes = new byte[8 + uuid.Length + randomBytes.Length];

            Array.Copy(BitConverter.GetBytes(timestamp), 0, combinedBytes, 0, 8);
            Array.Copy(uuid, 0, combinedBytes, 8, uuid.Length);
            Array.Copy(randomBytes, 0, combinedBytes, 8 + uuid.Length, randomBytes.Length);

            var hashedBytes = SHA3_256.IsSupported ? SHA3_256.HashData(combinedBytes) : SHA256.HashData(combinedBytes);
            
            return Result<byte[]>.Success(hashedBytes);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure("Failed to generate serial number.", exception: ex);
        }
    }
}
