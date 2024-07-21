// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Services;

public class CertificateService(IHostInformationService hostInformationService, ICaCertificateService caCertificateService, ISerialNumberService serialNumberService) : ICertificateService
{
    /// <inheritdoc />
    public Result<X509Certificate2> IssueCertificate(byte[] csrBytes)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(csrBytes);

            var caCertificate = caCertificateService.GetCaCertificate(X509ContentType.Pfx);

            var csr = CertificateRequest.LoadSigningRequest(csrBytes, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);
            var basicConstraints = csr.CertificateExtensions.OfType<X509BasicConstraintsExtension>().FirstOrDefault();

            if (basicConstraints?.CertificateAuthority == true)
            {
                Log.Error("CSR for CA certificates are not allowed.");
                return Result<X509Certificate2>.Failure("CSR for CA certificates are not allowed.", exception: new InvalidOperationException("CSR for CA certificates are not allowed."));
            }

            var rsaPrivateKey = caCertificate.GetRSAPrivateKey();

            if (rsaPrivateKey == null)
            {
                Log.Error("The RSA private key for the CA certificate could not be retrieved.");
                
                return Result<X509Certificate2>.Failure("The RSA private key for the CA certificate could not be retrieved.", exception: new InvalidOperationException("The RSA private key for the CA certificate could not be retrieved."));
            }

            var signatureGenerator = X509SignatureGenerator.CreateForRSA(rsaPrivateKey, RSASignaturePadding.Pkcs1);

            var notBefore = DateTimeOffset.UtcNow;
            var notAfter = DateTimeOffset.UtcNow.AddYears(1);

            var serialNumberResult = serialNumberService.GenerateSerialNumber();

            if (!serialNumberResult.IsSuccess)
            {
                Log.Error("Failed to generate serial number: {Message}", serialNumberResult.Errors.FirstOrDefault()?.Message);
                
                return Result<X509Certificate2>.Failure("Failed to generate serial number.", exception: new InvalidOperationException("Failed to generate serial number."));
            }

            var serialNumber = serialNumberResult.Value;

            var hostInformation = hostInformationService.GetHostInformation();

            var crlDistributionPoints = new List<string>
            {
                $"http://{hostInformation.Name}/crl"
            };

            var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints);

            csr.CertificateExtensions.Add(crlDistributionPointExtension);

            var certificate = csr.Create(caCertificate.SubjectName, signatureGenerator, notBefore, notAfter, serialNumber);

            return Result<X509Certificate2>.Success(certificate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while issuing a certificate.");

            return Result<X509Certificate2>.Failure("An error occurred while issuing a certificate.", exception: ex);
        }
    }
}