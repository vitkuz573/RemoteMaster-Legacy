// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class CertificateLoaderService : ICertificateLoaderService
{
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPathProvider _applicationPathProvider;
    private readonly ILogger<CertificateLoaderService> _logger;

    private X509Certificate2? _currentCertificate;

    public CertificateLoaderService(IFileSystem fileSystem, IApplicationPathProvider applicationPathProvider, ILogger<CertificateLoaderService> logger)
    {
        _fileSystem = fileSystem;
        _applicationPathProvider = applicationPathProvider;
        _logger = logger;

        LoadCertificateAsync().GetAwaiter().GetResult();
    }

    public X509Certificate2? GetCurrentCertificate()
    {
        return _currentCertificate;
    }

    public async Task LoadCertificateAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                LoadCertificateFromStore();
            }
            else
            {
                await LoadCertificateFromFileAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred while loading the certificate: {ExceptionMessage}", ex.Message);
        }
    }

    private void LoadCertificateFromStore()
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

    private async Task LoadCertificateFromFileAsync()
    {
        try
        {
            var certPath = _fileSystem.Path.Combine(_applicationPathProvider.DataDirectory, "device_certificate.pem");
            var keyPath = _fileSystem.Path.Combine(_applicationPathProvider.DataDirectory, "device_privatekey.pem");

            _logger.LogInformation("Attempting to load certificate from files: {CertPath}, {KeyPath}", certPath, keyPath);

            if (!_fileSystem.File.Exists(certPath))
            {
                _logger.LogWarning("Certificate file not found at {CertPath}.", certPath);

                return;
            }

            if (!_fileSystem.File.Exists(keyPath))
            {
                _logger.LogWarning("Private key file not found at {KeyPath}.", keyPath);
            }

            var keyPem = await _fileSystem.File.ReadAllTextAsync(keyPath);

            var certificate = X509CertificateLoader.LoadCertificateFromFile(certPath);

#pragma warning disable CA2000
            var rsa = RSA.Create();
#pragma warning restore CA2000
            rsa.ImportFromPem(keyPem);

            var certificateWithPrivateKey = certificate.CopyWithPrivateKey(rsa);

            if (!certificateWithPrivateKey.HasPrivateKey)
            {
                _logger.LogWarning("Loaded certificate does not contain a private key.");
            }

            if (_currentCertificate == null || !_currentCertificate.Equals(certificateWithPrivateKey))
            {
                _currentCertificate = certificateWithPrivateKey;

                _logger.LogInformation("Certificate loaded successfully from files. Subject: {Subject}", _currentCertificate.Subject);
            }
            else
            {
                _logger.LogInformation("Loaded certificate is the same as the current one. No update necessary.");
            }
        }
        catch (Exception e)
        {
            _logger.LogError("An error occurred while loading the certificate from files: {ExceptionMessage}", e.Message);
        }
    }
}
