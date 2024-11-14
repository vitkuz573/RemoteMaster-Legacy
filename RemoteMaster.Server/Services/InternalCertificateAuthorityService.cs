// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Services;

public class InternalCertificateAuthorityService(IOptions<InternalCertificateOptions> options, ISubjectService subjectService, IHostInformationService hostInformationService, ILogger<InternalCertificateAuthorityService> logger) : ICertificateAuthorityService
{
    private readonly InternalCertificateOptions _options = options.Value;

    public Result EnsureCaCertificateExists()
    {
        logger.LogDebug("Starting CA certificate check.");

        try
        {
            var caCertResult = GetCaCertificate(X509ContentType.Pfx);

            if (caCertResult.IsSuccess)
            {
                var existingCert = caCertResult.Value;

                if (existingCert.NotAfter > DateTime.Now)
                {
                    logger.LogInformation("Existing CA certificate for '{Name}' is valid.", _options.CommonName);
                    return Result.Ok();
                }

                logger.LogWarning("CA certificate for '{Name}' has expired. Reissuing.", _options.CommonName);
                var generateResult = GenerateCertificate(existingCert.GetRSAPrivateKey(), true);

                return generateResult.IsFailed
                    ? Result.Fail("Failed to reissue the expired CA certificate.")
                    : Result.Ok();
            }

            logger.LogWarning("No valid CA certificate found. Generating new certificate for '{Name}'.", _options.CommonName);
            var generateNewResult = GenerateCertificate(null, false);

            return generateNewResult.IsFailed
                ? Result.Fail("Failed to generate new CA certificate.")
                : Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred during CA certificate check.");
            
            return Result.Fail("An unexpected error occurred during CA certificate check.").WithError(ex.Message);
        }
    }

    private Result GenerateCertificate(RSA? externalRsaProvider, bool reuseKey)
    {
        logger.LogDebug("Generating new CA certificate with reuseKey={ReuseKey}.", reuseKey);

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
                rsaProvider = new RSACryptoServiceProvider(_options.KeySize, cspParams);
#pragma warning restore CA2000
            }
            else
            {
                rsaProvider = externalRsaProvider;
            }

            if (rsaProvider == null)
            {
                logger.LogError("RSA provider is null.");
                
                return Result.Fail("RSA provider is null.");
            }

            var distinguishedName = subjectService.GetDistinguishedName(_options.CommonName, _options.Subject.Organization, _options.Subject.OrganizationalUnit, _options.Subject.Locality, _options.Subject.State, _options.Subject.Country);
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

            var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints);

            request.CertificateExtensions.Add(crlDistributionPointExtension);

            var caCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(_options.ValidityPeriod));
            caCert.FriendlyName = _options.CommonName;

            AddCertificateToStore(caCert, StoreName.Root, StoreLocation.LocalMachine);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate CA certificate.");
            
            return Result.Fail("Failed to generate CA certificate.").WithError(ex.Message);
        }
        finally
        {
            if (!reuseKey && rsaProvider != null && rsaProvider != externalRsaProvider)
            {
                rsaProvider.Dispose();
            }
        }
    }

    private void AddCertificateToStore(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
    {
        try
        {
            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);

            var isCertificateAlreadyAdded = store.Certificates
                .Find(X509FindType.FindByThumbprint, cert.Thumbprint, false)
                .Count > 0;

            if (isCertificateAlreadyAdded)
            {
                logger.LogInformation("Certificate with thumbprint {Thumbprint} is already in the {StoreName} store in {StoreLocation} location.", cert.Thumbprint, storeName, storeLocation);

                return;
            }

            store.Close();
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);

            logger.LogInformation("Certificate with thumbprint {Thumbprint} added to the {StoreName} store in {StoreLocation} location.", cert.Thumbprint, storeName, storeLocation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add certificate to store.");
        }
    }

    public Result<X509Certificate2> GetCaCertificate(X509ContentType contentType)
    {
        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, _options.CommonName, false);

            foreach (var cert in certificates.Where(cert => cert.HasPrivateKey))
            {
                var exportedCertData = cert.Export(contentType);

                var loadedCertificate = contentType switch
                {
                    X509ContentType.Cert => X509CertificateLoader.LoadCertificate(exportedCertData),
                    X509ContentType.Pfx => X509CertificateLoader.LoadPkcs12(exportedCertData, null, X509KeyStorageFlags.MachineKeySet, Pkcs12LoaderLimits.Defaults),
                    _ => throw new NotSupportedException($"Content type {contentType} is not supported.")
                };

                return Result.Ok(loadedCertificate);
            }

            return Result.Fail<X509Certificate2>("No valid CA certificate with a private key found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve CA certificate.");

            return Result.Fail<X509Certificate2>("Failed to retrieve CA certificate.").WithError(ex.Message);
        }
    }
}
