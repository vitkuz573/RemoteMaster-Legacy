// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CertificateService(IOptions<CertificateOptions> options, IHostInformationService hostInformationService) : ICertificateService
{
    private readonly CertificateOptions _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

    public X509Certificate2 IssueCertificate(byte[] csrBytes)
    {
        ArgumentNullException.ThrowIfNull(csrBytes);

        var caCertificate = GetPrivateCaCertificate();

        var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
        var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();
        
        if (basicConstraints?.CertificateAuthority == true)
        {
            Log.Error("CSR for CA certificates are not allowed.");
            
            throw new InvalidOperationException("CSR for CA certificates are not allowed.");
        }

        var rsaPrivateKey = caCertificate.GetRSAPrivateKey();
        var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsaPrivateKey, RSASignaturePadding.Pkcs1);
        
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);
        var serialNumber = GenerateSerialNumber();

        var hostInformation = hostInformationService.GetHostInformation();

        var crlDistributionPoints = new List<string>
        {
            $"http://{hostInformation.Name}/crl"
        };

        var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints, false);

        csr.CertificateExtensions.Add(crlDistributionPointExtension);

        return csr.Create(caCertificate.SubjectName, signatureGenerator, notBefore, notAfter, serialNumber);
    }

    public X509Certificate2 GetCaCertificate()
    {
        var caCertificate = GetPrivateCaCertificate();
        
        return new X509Certificate2(caCertificate.Export(X509ContentType.Cert));
    }

    private X509Certificate2 GetPrivateCaCertificate()
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        
        var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.CommonName, false);

        foreach (var cert in certificates)
        {
            if (cert.HasPrivateKey)
            {
                return cert;
            }
        }

        throw new InvalidOperationException("No valid CA certificate found.");
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
}
