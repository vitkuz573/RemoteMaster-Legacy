// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Services;

public class CertificateProvider(IOptions<InternalCertificateOptions> options, ICertificateStoreService certificateStoreService) : ICertificateProvider
{
    private readonly InternalCertificateOptions _options = options.Value;

    public Result<X509Certificate2> GetIssuerCertificate()
    {
        try
        {
            var certificates = certificateStoreService.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, _options.CommonName);

            var caCertificate = certificates.FirstOrDefault(cert => cert.HasPrivateKey);

            return caCertificate == null
                ? Result.Fail<X509Certificate2>($"CA certificate with CommonName '{_options.CommonName}' not found.")
                : Result.Ok(caCertificate.GetUnderlyingCertificate());
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Error while retrieving CA certificate.").CausedBy(ex));
        }
    }
}
