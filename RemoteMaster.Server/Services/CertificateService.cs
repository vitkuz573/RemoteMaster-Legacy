// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Services;

public class CertificateService : ICertificateService
{
    private readonly CertificateSettings _settings;
    private readonly ILogger<CertificateService> _logger;

    private const int CertificateValidityYears = 1;

    public CertificateService(IOptions<CertificateSettings> options, ILogger<CertificateService> logger)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public X509Certificate2 GenerateCertificateFromCSR(byte[] csrBytes)
    {
        if (csrBytes is null)
        {
            _logger.LogError($"{nameof(csrBytes)} are null.");
            throw new ArgumentNullException(nameof(csrBytes));
        }

        var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();

        if (basicConstraints != null && basicConstraints.CertificateAuthority)
        {
            _logger.LogError("CSR for CA certificates are not allowed.");
            throw new InvalidOperationException("CSR for CA certificates are not allowed.");
        }

        using var caCertificate = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword);
        var subjectName = caCertificate.SubjectName;
        var signatureGenerator = X509SignatureGenerator.CreateForRSA(caCertificate.GetRSAPrivateKey(), RSASignaturePadding.Pkcs1);
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = DateTimeOffset.UtcNow.AddYears(CertificateValidityYears);
        var serialNumber = GenerateSerialNumber();

        var certificate = csr.Create(subjectName, signatureGenerator, notBefore, notAfter, serialNumber);

        _logger.LogInformation("Certificate generated successfully.");

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
