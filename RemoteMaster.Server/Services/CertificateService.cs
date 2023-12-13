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

    public X509Certificate2 IssueCertificate(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes);

        Log.Information("Starting to process CSR for certificate issuance.");

        var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        Log.Information("CSR loaded successfully.");

        // Check for CA constraints
        var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();
        if (basicConstraints != null && basicConstraints.CertificateAuthority)
        {
            Log.Error("CSR for CA certificates are not allowed.");
            throw new InvalidOperationException("CSR for CA certificates are not allowed.");
        }

        // Load CA certificate
        using var caCertificate = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword);
        Log.Information("CA certificate loaded successfully.");

        // Prepare for signing
        var subjectName = caCertificate.SubjectName;
        var signatureGenerator = X509SignatureGenerator.CreateForRSA(caCertificate.GetRSAPrivateKey(), RSASignaturePadding.Pkcs1);
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);
        var serialNumber = GenerateSerialNumber();

        Log.Information("Generating new certificate with Serial Number: {SerialNumber}", BitConverter.ToString(serialNumber));

        // Create the new certificate
        var certificate = csr.Create(subjectName, signatureGenerator, notBefore, notAfter, serialNumber);

        Log.Information("New certificate generated successfully.");

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
