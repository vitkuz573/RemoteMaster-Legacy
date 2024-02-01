// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class CertificateRequestService : ICertificateRequestService
{
    public CertificateRequest GenerateCSR(X500DistinguishedName subjectName, List<string> ipAddresses, out RSA rsaKeyPair)
    {
        ArgumentNullException.ThrowIfNull(subjectName);
        ArgumentNullException.ThrowIfNull(ipAddresses);

        Log.Information("Starting CSR generation.");

        rsaKeyPair = RSA.Create(2048);

        var csr = new CertificateRequest(subjectName, rsaKeyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var sanBuilder = new SubjectAlternativeNameBuilder();
        
        foreach (var ipAddress in ipAddresses)
        {
            sanBuilder.AddIpAddress(IPAddress.Parse(ipAddress));
        }

        csr.CertificateExtensions.Add(sanBuilder.Build(true));

        var enhancedKeyUsages = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.1")
        };

        csr.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, true));

        Log.Debug("CSR with {SANCount} SAN entries and {OIDCount} OIDs generated for subject: {SubjectName}.", ipAddresses.Count, enhancedKeyUsages.Count, subjectName.Name);

        return csr;
    }
}
