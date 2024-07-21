// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class CertificateProvider(IOptions<CertificateOptions> options, ICertificateStoreService certificateStoreService) : ICertificateProvider
{
    private readonly CertificateOptions _settings = options.Value;

    public Result<X509Certificate2> GetIssuerCertificate()
    {
        try
        {
            var certificates = certificateStoreService.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, _settings.CommonName);

            var caCertificate = certificates.FirstOrDefault(cert => cert.HasPrivateKey);

            if (caCertificate == null)
            {
                return Result<X509Certificate2>.Failure($"CA certificate with CommonName '{_settings.CommonName}' not found.");
            }

            return Result<X509Certificate2>.Success(caCertificate.GetUnderlyingCertificate());
        }
        catch (Exception ex)
        {
            return Result<X509Certificate2>.Failure("Error while retrieving CA certificate.", exception: ex);
        }
    }
}
