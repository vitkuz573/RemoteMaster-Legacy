// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateRequestService : ICertificateRequestService
{
    private readonly ILogger<CertificateRequestService> _logger;

    public CertificateRequestService(ILogger<CertificateRequestService> logger)
    {
        _logger = logger;
    }

    public CertificateRequest GenerateCSR(string subjectName, List<string> ipAddresses, out RSA rsaKeyPair)
    {
        if (ipAddresses == null)
        {
            throw new ArgumentNullException(nameof(ipAddresses));
        }

        _logger.LogInformation("Starting CSR generation for subject: {SubjectName}", subjectName);

        rsaKeyPair = RSA.Create(2048);

        _logger.LogDebug("RSA key pair generated successfully with key size {KeySize}.", rsaKeyPair.KeySize);
        _logger.LogDebug("RSA key pair public key parameters: {PublicKeyParameters}", rsaKeyPair.ExportParameters(false));

        var subject = new X500DistinguishedName(subjectName);
        var csr = new CertificateRequest(subject, rsaKeyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        _logger.LogDebug("CSR Subject: {CSRSubject}", csr.SubjectName.Name);

        var sanBuilder = new SubjectAlternativeNameBuilder();

        foreach (var ipAddress in ipAddresses)
        {
            sanBuilder.AddIpAddress(IPAddress.Parse(ipAddress));
        }

        var sanExtension = sanBuilder.Build();
        csr.CertificateExtensions.Add(sanExtension);

        _logger.LogDebug("Added Subject Alternative Name extension with {SANCount} IP addresses.", ipAddresses.Count);

        foreach (var extension in csr.CertificateExtensions)
        {
            _logger.LogDebug("CSR Extension: {ExtensionOid} - {ExtensionFriendlyName} - {ExtensionRawData}", extension.Oid, extension.Oid?.FriendlyName ?? "Unknown", extension.RawData);
        }

        _logger.LogInformation("CSR generated successfully for subject: {SubjectName}.", subjectName);

        return csr;
    }
}
