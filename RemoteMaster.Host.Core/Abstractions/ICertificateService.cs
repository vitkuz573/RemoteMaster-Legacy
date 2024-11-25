// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface ICertificateService
{
    void ProcessCertificate(byte[] certificateBytes, RSA rsaKeyPair);

    Task GetCaCertificateAsync();

    void RemoveExistingCertificate();

    void RemoveCertificate();

    Task IssueCertificateAsync(HostConfiguration hostConfiguration, AddressDto organizationAddress);
}
