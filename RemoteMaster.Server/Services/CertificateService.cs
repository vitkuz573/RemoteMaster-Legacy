// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;
using RemoteMaster.Server.Options;
using RemoteMaster.Shared.Abstractions;
using Serilog;

namespace RemoteMaster.Server.Services;

#pragma warning disable

public class CertificateService(IHostInformationService hostInformationService, ICertificateAuthorityService certificateAuthorityService, IOptions<ActiveDirectoryOptions> options) : ICertificateService
{
    private readonly ActiveDirectoryOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<Result<X509Certificate2>> IssueCertificate(byte[] csrBytes)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(csrBytes);

            var caCertificateResult = certificateAuthorityService.GetCaCertificate(X509ContentType.Pfx);

            if (!caCertificateResult.IsSuccess)
            {
                Log.Error("Failed to retrieve the CA certificate: {Message}", caCertificateResult.Errors.FirstOrDefault()?.Message);
                
                return Result.Fail<X509Certificate2>("Failed to retrieve the CA certificate.");
            }

            var caCertificate = caCertificateResult.Value;

            if (certificateAuthorityService is ActiveDirectoryCertificateAuthorityService)
            {
                Log.Information("Using Active Directory CA to issue the certificate.");

                return await IssueCertificateUsingWebEnrollment(csrBytes);
            }
            else
            {
                Log.Information("Using Internal CA to issue the certificate.");

                return IssueCertificateUsingInternal(csrBytes, caCertificate);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while issuing a certificate.");

            return Result.Fail(new Error("An error occurred while issuing a certificate.").CausedBy(ex));
        }
    }

    private Result<X509Certificate2> IssueCertificateUsingInternal(byte[] csrBytes, X509Certificate2 caCertificate)
    {
        try
        {
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
            var crlDistributionPoints = new List<string> { $"http://{hostInformation.Name}/crl" };
            var crlDistributionPointExtension = CertificateRevocationListBuilder.BuildCrlDistributionPointExtension(crlDistributionPoints);
            csr.CertificateExtensions.Add(crlDistributionPointExtension);

            var certificate = csr.Create(caCertificate.SubjectName, signatureGenerator, notBefore, notAfter, serialNumber.ToByteArray());
            
            return Result.Ok(certificate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while issuing a certificate using Internal CA.");
            
            return Result.Fail<X509Certificate2>(new Error("An error occurred while issuing a certificate using Internal CA.").CausedBy(ex));
        }
    }

    public async Task<Result<X509Certificate2>> IssueCertificateUsingWebEnrollment(byte[] csrBytes)
    {
        var baseUrl = $"http://{_options.Server}/certsrv/";

        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
        };

        using (var client = new HttpClient(handler))
        {
            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Mode", "newreq"),
                new KeyValuePair<string, string>("CertRequest", $"{Convert.ToBase64String(csrBytes)}"),
                new KeyValuePair<string, string>("CertAttrib", $"CertificateTemplate:{_options.TemplateName}"),
                new KeyValuePair<string, string>("TargetStoreFlags", "0"),
                new KeyValuePair<string, string>("SaveCert", "yes")
            });

            var response = await client.PostAsync($"{baseUrl}certfnsh.asp", requestBody);
            var responseBody = await response.Content.ReadAsStringAsync();

            var match = Regex.Match(responseBody, @"ReqID=(\d+)");

            var certResponse = await client.GetAsync($"{baseUrl}certnew.cer?ReqID={match.Groups[1].Value}&Enc=b64");
            var certBytes = await certResponse.Content.ReadAsByteArrayAsync();

            return new X509Certificate2(certBytes);
        }
    }
}
