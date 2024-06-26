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

public class CaCertificateService(IOptions<CertificateOptions> options, ISubjectService subjectService, IHostInformationService hostInformationService) : ICaCertificateService
{
    private readonly CertificateOptions _settings = options.Value;

    public void EnsureCaCertificateExists()
    {
        Log.Debug("Starting CA certificate check.");

        try
        {
            using var existingCert = GetCaCertificate(X509ContentType.Pfx);

            if (existingCert.NotAfter > DateTime.Now)
            {
                Log.Information("Existing CA certificate for '{Name}' is valid.", _settings.CommonName);
                return;
            }
            else
            {
                Log.Warning("CA certificate for '{Name}' has expired. Reissuing.", _settings.CommonName);
                GenerateCertificate(existingCert.GetRSAPrivateKey(), true);
            }
        }
        catch (InvalidOperationException ex)
        {
            Log.Warning(ex.Message);
            Log.Warning("No valid CA certificate found. Generating new certificate for '{Name}'.", _settings.CommonName);

            GenerateCertificate(null, false);
        }
    }

    private X509Certificate2 GenerateCertificate(RSA? externalRsaProvider, bool reuseKey)
    {
        Log.Debug("Generating new CA certificate with reuseKey={ReuseKey}.", reuseKey);

        RSA? rsaProvider = null;

        try
        {
            if (!reuseKey)
            {
                var cspParams = new CspParameters
                {
                    KeyContainerName = Guid.NewGuid().ToString(),
                    Flags = CspProviderFlags.UseMachineKeyStore,
                    KeyNumber = (int)KeyNumber.Exchange
                };

#pragma warning disable CA2000
                rsaProvider = new RSACryptoServiceProvider(_settings.KeySize, cspParams);
#pragma warning restore CA2000
            }
            else
            {
                rsaProvider = externalRsaProvider;
            }

            if (rsaProvider == null)
            {
                Log.Error("RSA provider is null.");

                throw new InvalidOperationException("RSA provider is null.");
            }

            var distinguishedName = subjectService.GetDistinguishedName(_settings.CommonName);
            var request = new CertificateRequest(distinguishedName, rsaProvider, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var crlFilePath = Path.Combine(programDataPath, "RemoteMaster", "list.crl");

            var hostInformation = hostInformationService.GetHostInformation();

            var crlDistributionPoints = new List<string>
                {
                    $"file:///{crlFilePath.Replace("\\", "/")}",
                    $"http://{hostInformation.Name}/crl"
                };

            var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints, false);

            request.CertificateExtensions.Add(crlDistributionPointExtension);

            var caCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(_settings.ValidityPeriod));

            caCert.FriendlyName = _settings.CommonName;

            AddCertificateToStore(caCert, StoreName.Root, StoreLocation.LocalMachine);

            return caCert;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate CA certificate.");
            
            throw;
        }
        finally
        {
            if (!reuseKey && rsaProvider != null && rsaProvider != externalRsaProvider)
            {
                rsaProvider.Dispose();
            }
        }
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

    public X509Certificate2 GetCaCertificate(X509ContentType contentType)
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, _settings.CommonName, false);

        foreach (var cert in certificates)
        {
            if (cert.HasPrivateKey)
            {
                if (contentType == X509ContentType.Cert)
                {
                    return new X509Certificate2(cert.Export(X509ContentType.Cert));
                }
                else
                {
                    return cert;
                }
            }
        }

        throw new InvalidOperationException("No valid CA certificate with a private key found.");
    }
}
