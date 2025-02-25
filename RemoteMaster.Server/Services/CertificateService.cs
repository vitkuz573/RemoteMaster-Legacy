// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using FluentResults;
using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;
using RemoteMaster.Server.Enums;
using RemoteMaster.Server.Options;
using RemoteMaster.Shared.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Cryptography.Certificates;

namespace RemoteMaster.Server.Services;

public class CertificateService(IHostInformationService hostInformationService, ICertificateAuthorityService certificateAuthorityService, IOptions<ActiveDirectoryOptions> options, ILogger<CertificateService> logger) : ICertificateService
{
    private readonly ActiveDirectoryOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<Result<X509Certificate2>> IssueCertificateAsync(byte[] csrBytes)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(csrBytes);

            var caCertificateResult = await certificateAuthorityService.GetCaCertificateAsync(X509ContentType.Pfx);

            if (!caCertificateResult.IsSuccess)
            {
                logger.LogError("Failed to retrieve the CA certificate: {Message}", caCertificateResult.Errors.FirstOrDefault()?.Message);
                
                return Result.Fail<X509Certificate2>("Failed to retrieve the CA certificate.");
            }

            var caCertificate = caCertificateResult.Value;

            if (certificateAuthorityService is ActiveDirectoryCertificateAuthorityService)
            {
                logger.LogInformation("Using Active Directory CA to issue the certificate.");

                return _options.Method switch
                {
                    ActiveDirectoryMethod.WebEnrollment => await IssueCertificateUsingWebEnrollmentAsync(csrBytes),
                    ActiveDirectoryMethod.CertEnroll => IssueCertificateUsingCertEnroll(csrBytes),
                    _ => throw new InvalidOperationException("Unknown method.")
                };
            }

            logger.LogInformation("Using Internal CA to issue the certificate.");

            return IssueCertificateUsingInternal(csrBytes, caCertificate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while issuing a certificate.");

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
                logger.LogError("CSR for CA certificates are not allowed.");
                
                return Result.Fail<X509Certificate2>("CSR for CA certificates are not allowed.");
            }

            var rsaPrivateKey = caCertificate.GetRSAPrivateKey();

            if (rsaPrivateKey == null)
            {
                logger.LogError("The RSA private key for the CA certificate could not be retrieved.");
                
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
            logger.LogError(ex, "An error occurred while issuing a certificate using Internal CA.");
            
            return Result.Fail<X509Certificate2>(new Error("An error occurred while issuing a certificate using Internal CA.").CausedBy(ex));
        }
    }

    private async Task<Result<X509Certificate2>> IssueCertificateUsingWebEnrollmentAsync(byte[] csrBytes)
    {
        var baseUrl = $"http://{_options.Server}/certsrv/";

        using var handler = new HttpClientHandler();
        handler.Credentials = new NetworkCredential(_options.Username, _options.Password);

        using var client = new HttpClient(handler);
        using var requestBody = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Mode", "newreq"),
            new KeyValuePair<string, string>("CertRequest", $"{Convert.ToBase64String(csrBytes)}"),
            new KeyValuePair<string, string>("CertAttrib", $"CertificateTemplate:{_options.TemplateName}"),
            new KeyValuePair<string, string>("TargetStoreFlags", "0"),
            new KeyValuePair<string, string>("SaveCert", "yes")
        ]);

        var response = await client.PostAsync($"{baseUrl}certfnsh.asp", requestBody);
        var responseBody = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(responseBody, @"ReqID=(\d+)");

        var certResponse = await client.GetAsync($"{baseUrl}certnew.cer?ReqID={match.Groups[1].Value}&Enc=b64");
        var certBytes = await certResponse.Content.ReadAsByteArrayAsync();

        var certificate = X509CertificateLoader.LoadCertificate(certBytes);

        return Result.Ok(certificate);
    }

    private Result<X509Certificate2> IssueCertificateUsingCertEnroll(byte[] csrBytes)
    {
        try
        {
            var certRequestPkcs10 = CreateInstance<IX509CertificateRequestPkcs10>("728AB342-217D-11DA-B2A4-000E7BBB2B09") ?? throw new InvalidOperationException("Failed to create IX509CertificateRequestPkcs10 instance.");
            var csrBase64 = Convert.ToBase64String(csrBytes);
            var bstrCsr = Marshal.StringToBSTR(csrBase64);

            try
            {
                certRequestPkcs10.InitializeDecode((BSTR)bstrCsr, EncodingType.XCN_CRYPT_STRING_BASE64);
            }
            finally
            {
                Marshal.FreeBSTR(bstrCsr);
            }

            var enrollment = CreateInstance<IX509Enrollment>("728AB340-217D-11DA-B2A4-000E7BBB2B09") ?? throw new InvalidOperationException("Failed to create IX509Enrollment instance.");
            enrollment.InitializeFromRequest(certRequestPkcs10);
            enrollment.Enroll();

            BSTR responseBstr;

            unsafe
            {
                enrollment.get_Response(EncodingType.XCN_CRYPT_STRING_BASE64, &responseBstr);
            }

            try
            {
                var certBytes = Convert.FromBase64String(responseBstr.ToString());
                var certificate = X509CertificateLoader.LoadCertificate(certBytes);

                return Result.Ok(certificate);
            }
            finally
            {
                Marshal.FreeBSTR(responseBstr);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while issuing a certificate using CertEnroll.");

            return Result.Fail<X509Certificate2>(new Error("An error occurred while issuing a certificate using CertEnroll.").CausedBy(ex));
        }
    }

    private static T? CreateInstance<T>(string clsid) where T : class
    {
        try
        {
            var type = Type.GetTypeFromCLSID(new Guid(clsid));

            return type == null
                ? throw new InvalidOperationException($"The type for CLSID {clsid} could not be found.")
                : (T?)Activator.CreateInstance(type);
        }
        catch (COMException ex) when (ex.ErrorCode == unchecked((int)0x80040154))
        {
            throw new InvalidOperationException($"{typeof(T).Name} COM object is not registered. Please ensure that all necessary components are installed and registered correctly.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"An unexpected error occurred while trying to create the {typeof(T).Name} instance.", ex);
        }
    }
}
