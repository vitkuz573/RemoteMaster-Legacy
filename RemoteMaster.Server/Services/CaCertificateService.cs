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
        X509Certificate2 caCert;

        var directoryPath = Path.GetDirectoryName(_settings.PfxPath);

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath ?? string.Empty);
            Log.Information("Created directory path for certificate storage: '{DirectoryPath}'.", directoryPath);
        }

        if (File.Exists(_settings.PfxPath))
        {
            Log.Information("CA certificate already exists at '{PfxPath}'. Loading existing certificate.", _settings.PfxPath);
            caCert = new X509Certificate2(_settings.PfxPath, _settings.PfxPassword, X509KeyStorageFlags.Exportable);
        }
        else
        {
            Log.Information("Starting CA certificate generation for '{Name}'.", _settings.Name);

            using var rsa = RSA.Create(4096);

            var subjectName = new X500DistinguishedName($"CN={_settings.Name}");
            var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

            caCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

            var pfxBytes = caCert.Export(X509ContentType.Pfx, _settings.PfxPassword);
            File.WriteAllBytes(_settings.PfxPath, pfxBytes);

            Log.Information("CA certificate for '{Name}' generated and saved to {PfxPath}.", _settings.Name, _settings.PfxPath);
        }

        AddCertificateToStore(caCert, StoreName.Root, StoreLocation.CurrentUser);

        return caCert;
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
