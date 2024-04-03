// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Abstractions;

public interface ICrlService
{
    Task RevokeCertificateAsync(string serialNumber, X509RevocationReason reason);

    Task<byte[]> GenerateCrlAsync();

    void PublishCrl(byte[] crlData);

    Task<CrlMetadata> GetCrlMetadataAsync();
}
