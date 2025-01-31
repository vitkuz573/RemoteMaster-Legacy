// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class CertificateService(IApiService apiService, ISubjectService subjectService, IFileSystem fileSystem, IApplicationPathProvider applicationPathProvider, ICertificateRequestService certificateRequestService, ICertificateLoaderService certificateLoaderService, ILogger<CertificateService> logger) : ICertificateService
{
    public async Task IssueCertificateAsync(HostConfiguration hostConfiguration, AddressDto organizationAddress)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(organizationAddress);

        RSA? rsaKeyPair = null;

        try
        {
            var distinguishedName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name, hostConfiguration.Subject.Organization, hostConfiguration.Subject.OrganizationalUnit, organizationAddress.Locality, organizationAddress.State, organizationAddress.Country);

            RemoveCertificates();

            var signingRequest = certificateRequestService.GenerateSigningRequest(distinguishedName, [hostConfiguration.Host.IpAddress], out rsaKeyPair);

            logger.LogInformation("Attempting to issue certificate...");

            var certificate = await apiService.IssueCertificateAsync(signingRequest);

            if (certificate == null || certificate.Length == 0)
            {
                throw new InvalidOperationException("Certificate processing failed.");
            }

            await ProcessCertificate(certificate, rsaKeyPair);
        }
        catch (Exception ex)
        {
            logger.LogError("Issuing certificate failed: {Message}.", ex.Message);
        }
        finally
        {
            rsaKeyPair?.Dispose();
        }
    }
    
    public async Task GetCaCertificateAsync()
    {
        try
        {
            logger.LogInformation("Requesting CA certificate's public part...");

            var caCertificateData = await apiService.GetCaCertificateAsync();

            if (caCertificateData == null || caCertificateData.Length == 0)
            {
                logger.LogError("Received CA certificate is null or empty.");

                throw new InvalidOperationException("Failed to request or process CA certificate.");
            }

            try
            {
                using var caCertificate = X509CertificateLoader.LoadCertificate(caCertificateData);

                LogCertificateDetails(caCertificate);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(caCertificate);

                    logger.LogInformation("CA certificate imported successfully into the certificate store.");
                }
                else
                {
                    const string caCertPath = "/etc/ca-certificates/remotemaster.pem";

                    await fileSystem.File.WriteAllTextAsync(caCertPath, caCertificate.ExportCertificatePem());

                    logger.LogInformation("CA certificate saved successfully at {Path}.", caCertPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("An error occurred while importing the CA certificate: {ErrorMessage}.", ex.Message);
                throw;
            }

            logger.LogInformation("CA certificate's public part received and processed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to get CA certificate: {Message}.", ex.Message);
        }
    }

    public void RemoveCertificates()
    {
        logger.LogInformation("Starting the process of removing existing certificates...");

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                var existingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

                if (existingCertificates.Count > 0)
                {
                    logger.LogInformation("Found {Count} certificates to remove.", existingCertificates.Count);

                    store.RemoveRange(existingCertificates);
                    logger.LogInformation("Successfully removed all certificates.");
                }
                else
                {
                    logger.LogInformation("No certificates found to remove.");
                }
            }
            else
            {
                var certPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "device_certificate.pem");
                var keyPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "device_privatekey.pem");

                if (fileSystem.File.Exists(certPath))
                {
                    fileSystem.File.Delete(certPath);

                    logger.LogInformation("Removed existing device certificate at {Path}.", certPath);
                }
                else
                {
                    logger.LogInformation("No existing device certificate found at {Path}.", certPath);
                }

                if (fileSystem.File.Exists(keyPath))
                {
                    fileSystem.File.Delete(keyPath);
                    logger.LogInformation("Removed existing private key at {Path}.", keyPath);
                }
                else
                {
                    logger.LogInformation("No existing private key found at {Path}.", keyPath);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError("Failed to remove certificates: {Message}", e.Message);
        }

        logger.LogInformation("Finished removing existing certificates.");
    }

    private async Task ProcessCertificate(byte[] certificateBytes, RSA rsaKeyPair)
    {
        ArgumentNullException.ThrowIfNull(certificateBytes);
        ArgumentNullException.ThrowIfNull(rsaKeyPair);

        X509Certificate2? certificateWithPrivateKey = null;

        try
        {
            if (certificateBytes.Length == 0)
            {
                logger.LogError("Certificate bytes are empty.");

                return;
            }

            logger.LogInformation("Received certificate bytes, starting processing...");

            certificateWithPrivateKey = X509CertificateLoader.LoadPkcs12(certificateBytes, null, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable).CopyWithPrivateKey(rsaKeyPair);

            logger.LogInformation("Certificate with private key prepared.");

            LogCertificateDetails(certificateWithPrivateKey);

            var pfxBytes = certificateWithPrivateKey.Export(X509ContentType.Pfx, (string?)null);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var importedCertificate = X509CertificateLoader.LoadPkcs12(pfxBytes, null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

                using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(importedCertificate);

                logger.LogInformation("Certificate with private key imported successfully into the certificate store.");
            }
            else
            {
                var certPem = certificateWithPrivateKey.ExportCertificatePem();
                var keyPem = rsaKeyPair.ExportRSAPrivateKeyPem();

                var certPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "device_certificate.pem");
                var keyPath = fileSystem.Path.Combine(applicationPathProvider.DataDirectory, "device_privatekey.pem");

                await fileSystem.File.WriteAllTextAsync(certPath, certPem);
                await fileSystem.File.WriteAllTextAsync(keyPath, keyPem);

                logger.LogInformation("Certificate and private key saved successfully at {CertPath} and {KeyPath}.", certPath, keyPath);
            }

            certificateLoaderService.LoadCertificate();
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred while processing the certificate: {ErrorMessage}.", ex.Message);
        }
        finally
        {
            certificateWithPrivateKey?.Dispose();
        }
    }

    private void LogCertificateDetails(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        logger.LogInformation("Certificate Details:");

        logger.LogInformation("    Subject: {Subject}", certificate.Subject);
        logger.LogInformation("    Issuer: {Issuer}", certificate.Issuer);
        logger.LogInformation("    Valid From: {ValidFrom:O}", certificate.NotBefore);
        logger.LogInformation("    Valid To: {ValidTo:O}", certificate.NotAfter);
        logger.LogInformation("    Serial Number: {SerialNumber}", certificate.SerialNumber);
        logger.LogInformation("    Thumbprint: {Thumbprint}", certificate.Thumbprint);
        logger.LogInformation("    Version: {Version}", certificate.Version);
        logger.LogInformation("    Signature Algorithm: {Algorithm}", certificate.SignatureAlgorithm.FriendlyName);
        logger.LogInformation("    Public Key Algorithm: {Algorithm}", certificate.PublicKey.Oid.FriendlyName);

        var rsaKey = certificate.GetRSAPublicKey();

        if (rsaKey != null)
        {
            logger.LogInformation("    Public Key Length: {KeySize} bits", rsaKey.KeySize);
        }

        logger.LogInformation("    Has Private Key: {HasPrivateKey}", certificate.HasPrivateKey);
    }
}
