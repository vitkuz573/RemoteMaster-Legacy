// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface ICrlService
{
    Task RevokeCertificateAsync(string serialNumber, X509RevocationReason reason);

    Task<byte[]> GenerateCrlAsync();

    Task<bool> PublishCrlAsync(byte[] crlData);

    Task<CrlMetadata> GetCrlMetadataAsync();
}
