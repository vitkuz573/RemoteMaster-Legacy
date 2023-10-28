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

    public CertificateService(IOptions<CertificateSettings> options, ILogger<CertificateService> logger)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _settings = options.Value;
        _logger = logger;
    }

    public X509Certificate2 GenerateCertificateFromCSR(byte[] csrBytes)
    {
        if (csrBytes == null)
        {
            throw new ArgumentNullException(nameof(csrBytes));
        }

        var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256);

        using var caCertificate = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword);
        var subjectName = new X500DistinguishedName(caCertificate.SubjectName);
        var signatureGenerator = X509SignatureGenerator.CreateForRSA(caCertificate.GetRSAPrivateKey(), RSASignaturePadding.Pkcs1);
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);
        var serialNumber = GenerateSerialNumber();

        var certificate = csr.Create(subjectName, signatureGenerator, notBefore, notAfter, serialNumber);

        foreach (var extension in csr.CertificateExtensions)
        {
            var asnData = new AsnEncodedData(extension.Oid, extension.RawData);
            var formattedData = asnData.Format(true);

            _logger.LogDebug("CSR Extension: {ExtensionOid} - {ExtensionFriendlyName} - {ExtensionFormattedData}", extension.Oid?.Value ?? "Unknown", extension.Oid?.FriendlyName ?? "Unknown", formattedData);
            
            certificate.Extensions.Add(extension);
        }

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