// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;
using RemoteMaster.Shared.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CertificateService(IHostInformationService hostInformationService, ICaCertificateService caCertificateService) : ICertificateService
{
    /// <inheritdoc />
    public Result<X509Certificate2> IssueCertificate(byte[] csrBytes)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(csrBytes);

            var caCertificateResult = caCertificateService.GetCaCertificate(X509ContentType.Pfx);

            if (!caCertificateResult.IsSuccess)
            {
                Log.Error("Failed to retrieve the CA certificate: {Message}", caCertificateResult.Errors.FirstOrDefault()?.Message);
                
                return Result.Fail<X509Certificate2>("Failed to retrieve the CA certificate.");
            }

            var caCertificate = caCertificateResult.Value;

            var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
            var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();

            if (basicConstraints?.CertificateAuthority == true)
            {
                Log.Error("CSR for CA certificates are not allowed.");
                
                return Result.Fail<X509Certificate2>("CSR for CA certificates are not allowed.");
            }

            var rsaPrivateKey = caCertificate.GetRSAPrivateKey();

            if (rsaPrivateKey == null)
            {
                Log.Error("The RSA private key for the CA certificate could not be retrieved.");
                
                return Result.Fail<X509Certificate2>("The RSA private key for the CA certificate could not be retrieved.");
            }

            var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsaPrivateKey, RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow;
            var notAfter = DateTimeOffset.UtcNow.AddYears(1);

            var serialNumber = SerialNumber.Generate();

            var hostInformation = hostInformationService.GetHostInformation();

            var crlDistributionPoints = new List<string>
            {
                $"http://{hostInformation.Name}/crl"
            };

            var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints);

            csr.CertificateExtensions.Add(crlDistributionPointExtension);

            var certificate = csr.Create(caCertificate.SubjectName, signatureGenerator, notBefore, notAfter, serialNumber.ToByteArray());

            return Result.Ok(certificate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while issuing a certificate.");
            
            return Result.Fail(new Error("An error occurred while issuing a certificate.").CausedBy(ex));
        }
    }
}
