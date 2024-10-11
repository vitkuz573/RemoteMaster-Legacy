// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateRequestService(ILogger<CertificateRequestService> logger) : ICertificateRequestService
{
    public byte[] GenerateSigningRequest(X500DistinguishedName subjectName, List<IPAddress> ipAddresses, out RSA rsaKeyPair)
    {
        ArgumentNullException.ThrowIfNull(subjectName);
        ArgumentNullException.ThrowIfNull(ipAddresses);

        logger.LogInformation("Starting CSR generation.");

        rsaKeyPair = RSA.Create(4096);

        var certificateRequest = new CertificateRequest(subjectName, rsaKeyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();

        foreach (var ipAddress in ipAddresses)
        {
            sanBuilder.AddIpAddress(ipAddress);
        }

        certificateRequest.CertificateExtensions.Add(sanBuilder.Build(true));

        var enhancedKeyUsages = new OidCollection
        {
            new("1.3.6.1.5.5.7.3.1")
        };

        certificateRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, true));

        logger.LogDebug("CSR with {SANCount} SAN entries and {OIDCount} OIDs generated.", ipAddresses.Count, enhancedKeyUsages.Count);

        var signingRequest = certificateRequest.CreateSigningRequest();

        return signingRequest;
    }
}
