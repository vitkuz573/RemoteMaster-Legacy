// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class CertificateService(IApiService apiService, ISubjectService subjectService, ICertificateRequestService certificateRequestService, ICertificateLoaderService certificateLoaderService, ILogger<CertificateService> logger) : ICertificateService
{
    public async Task IssueCertificateAsync(HostConfiguration hostConfiguration, AddressDto organizationAddress)
    {
        ArgumentNullException.ThrowIfNull(hostConfiguration);
        ArgumentNullException.ThrowIfNull(organizationAddress);

        RSA? rsaKeyPair = null;

        try
        {
            var ipAddresses = new List<IPAddress>
            {
                hostConfiguration.Host.IpAddress
            };

            var distinguishedName = subjectService.GetDistinguishedName(hostConfiguration.Host.Name, hostConfiguration.Subject.Organization, hostConfiguration.Subject.OrganizationalUnit, organizationAddress.Locality, organizationAddress.State, organizationAddress.Country);

            logger.LogInformation("Removing existing certificates...");

            RemoveCertificates();

            var signingRequest = certificateRequestService.GenerateSigningRequest(distinguishedName, ipAddresses, out rsaKeyPair);

            logger.LogInformation("Attempting to issue certificate...");

            var certificate = await apiService.IssueCertificateAsync(signingRequest);

            if (certificate == null || certificate.Length == 0)
            {
                throw new InvalidOperationException("Certificate processing failed.");
            }

            ProcessCertificate(certificate, rsaKeyPair);

            logger.LogInformation("Certificate issued and processed successfully.");
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

                using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);
                store.Add(caCertificate);

                logger.LogInformation("CA certificate imported successfully into the certificate store.");
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

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        var existingCertificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

        if (existingCertificates.Count > 0)
        {
            logger.LogInformation("Found {Count} certificates to remove.", existingCertificates.Count);

            foreach (var cert in existingCertificates)
            {
                try
                {
                    store.Remove(cert);
                    logger.LogInformation("Successfully removed certificate with serial number: {SerialNumber}.", cert.SerialNumber);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to remove certificate with serial number: {SerialNumber}. Error: {Message}", cert.SerialNumber, ex.Message);
                }
            }
        }
        else
        {
            logger.LogInformation("No certificates found to remove.");
        }

        logger.LogInformation("Finished removing existing certificates.");
    }

    private void ProcessCertificate(byte[] certificateBytes, RSA rsaKeyPair)
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

            using var importedCertificate = X509CertificateLoader.LoadPkcs12(pfxBytes, null, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(importedCertificate);

            logger.LogInformation("Certificate with private key imported successfully into the certificate store.");

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
        logger.LogInformation("Certificate Details:");

        logger.LogInformation("    Subject: {Subject}", certificate.Subject);
        logger.LogInformation("    Issuer: {Issuer}", certificate.Issuer);
        logger.LogInformation("    Valid From: {ValidFrom}", certificate.NotBefore);
        logger.LogInformation("    Valid To: {ValidTo}", certificate.NotAfter);
        logger.LogInformation("    Serial Number: {SerialNumber}", certificate.SerialNumber);
        logger.LogInformation("    Thumbprint: {Thumbprint}", certificate.Thumbprint);
        logger.LogInformation("    Version: {Version}", certificate.Version);
    }
}
