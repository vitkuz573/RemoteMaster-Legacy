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
        ArgumentNullException.ThrowIfNull(ipAddresses);

        Log.Information("Starting CSR generation for subject: {SubjectName}", subjectName);

        rsaKeyPair = RSA.Create(2048);

        Log.Debug("RSA key pair generated successfully with key size {KeySize}.", rsaKeyPair.KeySize);

        var csr = new CertificateRequest(subjectName, rsaKeyPair, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        Log.Debug("CSR Subject: {SubjectName}", csr.SubjectName.Name);

        var sanBuilder = new SubjectAlternativeNameBuilder();

        foreach (var ipAddress in ipAddresses)
        {
            sanBuilder.AddIpAddress(IPAddress.Parse(ipAddress));
        }

        var sanExtension = sanBuilder.Build(true);
        csr.CertificateExtensions.Add(sanExtension);

        Log.Debug("Added Subject Alternative Name extension with {SANCount} IP addresses.", ipAddresses.Count);

        var enhancedKeyUsages = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.1")
        };

        csr.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, true));

        Log.Debug("Added Enhanced Key Usage extension with {OIDCount} OIDs.", enhancedKeyUsages.Count);

        foreach (var extension in csr.CertificateExtensions)
        {
            var asnData = new AsnEncodedData(extension.Oid, extension.RawData);
            var formattedData = asnData.Format(true);

            Log.Debug("CSR Extension: {ExtensionOid} - {ExtensionFriendlyName} - {ExtensionFormattedData}", extension.Oid?.Value ?? "Unknown", extension.Oid?.FriendlyName ?? "Unknown", formattedData);
        }

        Log.Information("CSR generated successfully for subject: {SubjectName}.", subjectName);

        return csr;
    }
}
