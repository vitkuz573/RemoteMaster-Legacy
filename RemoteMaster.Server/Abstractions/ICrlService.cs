// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using FluentResults;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines the contract for managing Certificate Revocation Lists (CRLs).
/// </summary>
public interface ICrlService
{
    /// <summary>
    /// Revokes a certificate asynchronously.
    /// </summary>
    /// <param name="serialNumber">The serial number of the certificate to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> RevokeCertificateAsync(SerialNumber serialNumber, X509RevocationReason reason);

    /// <summary>
    /// Generates a CRL asynchronously.
    /// </summary>
    /// <returns>A result containing the CRL data or an error.</returns>
    Task<Result<byte[]>> GenerateCrlAsync();

    /// <summary>
    /// Publishes a CRL asynchronously.
    /// </summary>
    /// <param name="crlData">The CRL data to publish.</param>
    /// <param name="customPath">An optional custom path for publishing the CRL.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> PublishCrlAsync(byte[] crlData, string? customPath = null);
}
