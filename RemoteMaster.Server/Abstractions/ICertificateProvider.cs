// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using FluentResults;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Defines the contract for certificate providers.
/// </summary>
public interface ICertificateProvider
{
    /// <summary>
    /// Retrieves the issuer certificate based on the provided common name.
    /// </summary>
    /// <returns>
    /// A <see cref="Result{X509Certificate2}"/> that represents:
    /// <list type="bullet">
    ///     <item><description>If the certificate is found and accessible, it returns <see cref="Result{X509Certificate2}.Success"/> with the found certificate.</description></item>
    ///     <item><description>If the certificate is not found or an error occurs while retrieving it, it returns <see cref="Result{X509Certificate2}.Failure"/> with an appropriate error message.</description></item>
    /// </list>
    /// </returns>
    Result<X509Certificate2> GetIssuerCertificate();
}
