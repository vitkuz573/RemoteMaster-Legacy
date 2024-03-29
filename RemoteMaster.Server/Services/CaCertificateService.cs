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

public class CaCertificateService(IOptions<CaCertificateOptions> options) : ICaCertificateService
{
    private readonly CaCertificateOptions _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

    public X509Certificate2 CreateCaCertificate()
    {
        var existingCert = FindExistingCertificate();

        if (existingCert != null)
        {
            Log.Information("Existing CA certificate for '{Name}' found.", _settings.Name);

            return existingCert;
        }

        Log.Information("Starting CA certificate generation for '{Name}'.", _settings.Name);

        var cspParams = new CspParameters
        {
            KeyContainerName = Guid.NewGuid().ToString(),
            Flags = CspProviderFlags.UseMachineKeyStore,
            KeyNumber = (int)KeyNumber.Exchange
        };

        using var rsaProvider = new RSACryptoServiceProvider(4096, cspParams);

        var subjectName = new X500DistinguishedName($"CN={_settings.Name}");
        var request = new CertificateRequest(subjectName, rsaProvider, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

        var caCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

        caCert.FriendlyName = _settings.Name;

        Log.Information("CA certificate for '{Name}' generated.", _settings.Name);

        AddCertificateToStore(caCert, StoreName.Root, StoreLocation.LocalMachine);

        return caCert;
    }

    private X509Certificate2 FindExistingCertificate()
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);

        store.Open(OpenFlags.ReadOnly);

        var existingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.Name, false);

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
