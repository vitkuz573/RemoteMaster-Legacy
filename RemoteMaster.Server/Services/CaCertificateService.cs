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

public class CaCertificateService(IOptions<CaCertificateOptions> options, ISubjectService subjectService) : ICaCertificateService
{
    private readonly CaCertificateOptions _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

    public X509Certificate2 CreateCaCertificate()
    {
        var existingCert = FindExistingCertificate();

        if (existingCert != null)
        {
            Log.Information("Existing CA certificate for '{Name}' found.", _settings.CommonName);

            return existingCert;
        }

        Log.Information("Starting CA certificate generation for '{Name}'.", _settings.CommonName);

        var cspParams = new CspParameters
        {
            KeyContainerName = Guid.NewGuid().ToString(),
            Flags = CspProviderFlags.UseMachineKeyStore,
            KeyNumber = (int)KeyNumber.Exchange
        };

        using var rsaProvider = new RSACryptoServiceProvider(_settings.RSAKeySize, cspParams);

        var distinguishedName = subjectService.GetDistinguishedName(_settings.CommonName);
        var request = new CertificateRequest(distinguishedName, rsaProvider, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

        var caCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(_settings.ValidityPeriod));

        caCert.FriendlyName = _settings.CommonName;

        Log.Information("CA certificate for '{Name}' generated.", _settings.CommonName);

        AddCertificateToStore(caCert, StoreName.Root, StoreLocation.LocalMachine);

        return caCert;
    }

    private X509Certificate2? FindExistingCertificate()
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);

        store.Open(OpenFlags.ReadOnly);

        var existingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.CommonName, false);

        return existingCertificates.Count > 0 ? existingCertificates[0] : null;
    }

    private static void AddCertificateToStore(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
    {
        using var store = new X509Store(storeName, storeLocation);

        store.Open(OpenFlags.ReadOnly);

        var isCertificateAlreadyAdded = store.Certificates
            .Find(X509FindType.FindByThumbprint, cert.Thumbprint, false)
            .Count > 0;

        if (isCertificateAlreadyAdded)
        {
            Log.Information("Certificate with thumbprint {Thumbprint} is already in the {StoreName} store in {StoreLocation} location.", cert.Thumbprint, storeName, storeLocation);
        }
        else
        {
            store.Close();
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);

            Log.Information("Certificate with thumbprint {Thumbprint} added to the {StoreName} store in {StoreLocation} location.", cert.Thumbprint, storeName, storeLocation);
        }

        store.Close();
    }
}
