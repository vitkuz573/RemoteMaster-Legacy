// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CaCertificateService(IOptions<CertificateOptions> options, ISubjectService subjectService) : ICaCertificateService
{
    private readonly CertificateOptions _settings = options.Value;

    public X509Certificate2 CreateCaCertificate()
    {
        var existingCert = FindExistingCertificate();

        if (existingCert != null)
        {
            if (existingCert.NotAfter > DateTime.Now)
            {
                Log.Information("Existing CA certificate for '{Name}' found.", _settings.CommonName);
                
                return existingCert;
            }
            else
            {
                Log.Information("Existing CA certificate for '{Name}' has expired. Reissuing with the same key.", _settings.CommonName);

                return GenerateCertificate(existingCert.GetRSAPrivateKey(), true);
            }
        }

        Log.Information("No existing CA certificate found or it has expired. Generating a new one for '{Name}'.", _settings.CommonName);

        return GenerateCertificate(null, false);
    }

    private X509Certificate2 GenerateCertificate(RSA? externalRsaProvider, bool reuseKey)
    {
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
                rsaProvider = new RSACryptoServiceProvider(_settings.RSAKeySize, cspParams);
#pragma warning restore CA2000
            }
            else
            {
                rsaProvider = externalRsaProvider;
            }

            var distinguishedName = subjectService.GetDistinguishedName(_settings.CommonName);
            var request = new CertificateRequest(distinguishedName, rsaProvider, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

            var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var crlFilePath = Path.Combine(programDataPath, "RemoteMaster", "list.crl");

            var crlDistributionPoints = new List<string>
            {
                $"file:///{crlFilePath.Replace("\\", "/")}",
                $"http://{GetLocalIpAddress()}:5254/crl"
            };

            var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints, false);
            
            request.CertificateExtensions.Add(crlDistributionPointExtension);

            var caCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(_settings.ValidityPeriod));
            
            caCert.FriendlyName = _settings.CommonName;

            AddCertificateToStore(caCert, StoreName.Root, StoreLocation.LocalMachine);

            return caCert;
        }
        finally
        {
            if (!reuseKey && rsaProvider != null && rsaProvider != externalRsaProvider)
            {
                rsaProvider.Dispose();
            }
        }
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

    private static string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}
