// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using FluentResults;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines the contract for certificate services.
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Issues a certificate based on the provided CSR (Certificate Signing Request) bytes.
    /// </summary>
    /// <param name="csrBytes">The CSR bytes.</param>
    /// <returns>A result containing the issued certificate or an error.</returns>
    Task<Result<X509Certificate2>> IssueCertificateAsync(byte[] csrBytes);
}
