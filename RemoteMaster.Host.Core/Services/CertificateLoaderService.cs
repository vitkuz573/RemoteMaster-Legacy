// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class CertificateLoaderService : ICertificateLoaderService
{
    private X509Certificate2 _currentCertificate;

    public CertificateLoaderService()
    {
        LoadCertificate();

        if (_currentCertificate == null)
        {
            throw new InvalidOperationException("No valid certificate found during initialization.");
        }
    }

    public X509Certificate2 GetCurrentCertificate()
    {
        return _currentCertificate ?? throw new InvalidOperationException("No valid certificate loaded.");
    }

    public void LoadCertificate()
    {
        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            Log.Information("Attempting to load certificates from store {StoreName} in {StoreLocation}.", store.Name, store.Location);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), validOnly: false);
            Log.Information("Found {Count} certificates with hostname {Hostname}.", certificates.Count, Dns.GetHostName());

            var newCertificate = certificates
                .OfType<X509Certificate2>()
                .FirstOrDefault(cert => cert.HasPrivateKey);

            if (newCertificate != null)
            {
                if (!newCertificate.Equals(_currentCertificate))
                {
                    _currentCertificate = newCertificate;

                    Log.Information("New certificate loaded successfully with subject: {Subject}.", _currentCertificate.Subject);
                }
                else
                {
                    Log.Information("Loaded certificate is the same as the current one. No update necessary.");
                }
            }
            else
            {
                Log.Warning("No valid certificates with private key were found.");
                
                throw new InvalidOperationException("No valid certificate with a private key found.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred while loading the certificate: {ExceptionMessage}", ex.Message);
            
            throw;
        }
    }
}
