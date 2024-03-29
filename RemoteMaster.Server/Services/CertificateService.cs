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
    private readonly CertificateOptions _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

    public X509Certificate2 IssueCertificate(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes);

        var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();

        if (basicConstraints is { CertificateAuthority: true })
        {
            Log.Error("CSR for CA certificates are not allowed.");
        }

        using var caCertificate = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword);
        var subjectName = caCertificate.SubjectName;
        var rsaPrivateKey = caCertificate.GetRSAPrivateKey();

        if (rsaPrivateKey == null)
        {
            Log.Error("Failed to obtain RSA private key from CA certificate.");
            throw new InvalidOperationException("CA certificate does not have an accessible RSA private key.");
        }

        var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsaPrivateKey, RSASignaturePadding.Pkcs1);
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = DateTimeOffset.UtcNow.AddMinutes(1);
        var serialNumber = GenerateSerialNumber();

        var certificate = csr.Create(subjectName, signatureGenerator, notBefore, notAfter, serialNumber);

        return certificate;
    }

    private static byte[] GenerateSerialNumber()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var uuid = Guid.NewGuid().ToByteArray();
        var randomBytes = new byte[16];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var combinedBytes = new byte[8 + uuid.Length + randomBytes.Length];
        Array.Copy(BitConverter.GetBytes(timestamp), 0, combinedBytes, 0, 8);
        Array.Copy(uuid, 0, combinedBytes, 8, uuid.Length);
        Array.Copy(randomBytes, 0, combinedBytes, 8 + uuid.Length, randomBytes.Length);

        return SHA3_256.IsSupported ? SHA3_256.HashData(combinedBytes) : SHA256.HashData(combinedBytes);
    }

    public X509Certificate2 GetCaCertificate()
    {
        using var caCertificate = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword, X509KeyStorageFlags.Exportable);
        var publicCert = new X509Certificate2(caCertificate.Export(X509ContentType.Cert));

        return publicCert;
    }
}
