// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateLoaderService : ICertificateLoaderService
{
    private readonly ILogger<CertificateLoaderService> _logger;

    private X509Certificate2? _currentCertificate;

    public CertificateLoaderService(ILogger<CertificateLoaderService> logger)
    {
        _logger = logger;

        LoadCertificate();
    }

    public X509Certificate2? GetCurrentCertificate()
    {
        return _currentCertificate;
    }

    public void LoadCertificate()
    {
        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            _logger.LogInformation("Attempting to load certificates from store {StoreName} in {StoreLocation}.", store.Name, store.Location);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);
            _logger.LogInformation("Found {Count} certificates with hostname {Hostname}.", certificates.Count, Dns.GetHostName());

            var newCertificate = certificates
                .FirstOrDefault(cert => cert.HasPrivateKey);

            if (newCertificate != null)
            {
                if (!newCertificate.Equals(_currentCertificate))
                {
                    _currentCertificate = newCertificate;

                    _logger.LogInformation("New certificate loaded successfully with subject: {Subject}.", _currentCertificate.Subject);
                }
                else
                {
                    _logger.LogInformation("Loaded certificate is the same as the current one. No update necessary.");
                }
            }
            else
            {
                _logger.LogWarning("No valid certificates with private key were found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while loading the certificate: {ExceptionMessage}", ex.Message);
        }
    }
}
