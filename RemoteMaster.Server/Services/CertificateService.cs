// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CertificateService(IOptions<CertificateOptions> options) : ICertificateService
{
    private readonly CertificateOptions _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly DateTimeOffset CertificateValidity = DateTimeOffset.UtcNow.AddYears(1);

    public X509Certificate2 IssueCertificate(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes);

        var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();

        if (basicConstraints != null && basicConstraints.CertificateAuthority)
        {
            Log.Error("CSR for CA certificates are not allowed.");
            throw new InvalidOperationException("CSR for CA certificates are not allowed.");
        }

        using var caCertificate = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword);
        var subjectName = caCertificate.SubjectName;
        var signatureGenerator = X509SignatureGenerator.CreateForRSA(caCertificate.GetRSAPrivateKey(), RSASignaturePadding.Pkcs1);
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = CertificateValidity;
        var serialNumber = GenerateSerialNumber();

        var certificate = csr.Create(subjectName, signatureGenerator, notBefore, notAfter, serialNumber);

        Log.Information("Certificate generated successfully.");

        return certificate;
    }

    private static byte[] GenerateSerialNumber()
    {
        var serialNumberBytes = new byte[16];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serialNumberBytes);
        }

        return serialNumberBytes;
    }
}
