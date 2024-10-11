// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Server.Tests;

public class CertificateServiceTests
{
    private readonly Mock<ICertificateAuthorityService> _certificateAuthorityServiceMock;
    private readonly Mock<IHostInformationService> _hostInformationServiceMock;
    private readonly CertificateService _certificateService;

    public CertificateServiceTests()
    {
        _certificateAuthorityServiceMock = new Mock<ICertificateAuthorityService>();
        _hostInformationServiceMock = new Mock<IHostInformationService>();

        var activeDirectoryOptions = Microsoft.Extensions.Options.Options.Create(new ActiveDirectoryOptions());

        Mock<ILogger<CertificateService>> loggerMock = new();

        _certificateService = new CertificateService(
            _hostInformationServiceMock.Object,
            _certificateAuthorityServiceMock.Object,
            activeDirectoryOptions,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task IssueCertificate_WithNullCsrBytes_ReturnsArgumentNullExceptionResult()
    {
        // Act
        var result = await _certificateService.IssueCertificate(null!);

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.Equal("An error occurred while issuing a certificate.", errorDetails.Message);
    }

    [Fact]
    public async Task IssueCertificate_WithCaCsr_ReturnsInvalidOperationExceptionResult()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(true);
        using var caCertificate = GenerateCaCertificate();

        _certificateAuthorityServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(Result.Ok(caCertificate));

        // Act
        var result = await _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.Equal("CSR for CA certificates are not allowed.", errorDetails.Message);
    }

    [Fact]
    public async Task IssueCertificate_WithValidCsr_ReturnsSuccessResult()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(false);
        using var caCertificate = GenerateCaCertificate();

        var ipAddress = IPAddress.Loopback;
        var macAddress = PhysicalAddress.Parse("00-14-22-01-23-45");

        var host = new HostDto("localhost", ipAddress, macAddress);

        _certificateAuthorityServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(Result.Ok(caCertificate));
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(host);

        // Act
        var result = await _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ValueOrDefault);
        Assert.Equal(caCertificate.SubjectName.Name, result.ValueOrDefault.Issuer);
    }

    [Fact]
    public async Task IssueCertificate_WithMinimalValidCsr_ReturnsSuccessResult()
    {
        // Arrange
        var csrBytes = GenerateMinimalValidCsrBytes();
        using var caCertificate = GenerateCaCertificate();

        var ipAddress = IPAddress.Loopback;
        var macAddress = PhysicalAddress.Parse("00-14-22-01-23-45");

        var host = new HostDto("localhost", ipAddress, macAddress);

        _certificateAuthorityServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(Result.Ok(caCertificate));
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(host);

        // Act
        var result = await _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ValueOrDefault);
        Assert.Equal(caCertificate.SubjectName.Name, result.ValueOrDefault.Issuer);
    }

    [Fact]
    public async Task IssueCertificate_WithCsrContainingExtensions_ReturnsSuccessResult()
    {
        // Arrange
        var csrBytes = GenerateCsrBytesWithExtensions();
        using var caCertificate = GenerateCaCertificate();

        var ipAddress = IPAddress.Loopback;
        var macAddress = PhysicalAddress.Parse("00-14-22-01-23-45");

        var host = new HostDto("localhost", ipAddress, macAddress);

        _certificateAuthorityServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(Result.Ok(caCertificate));
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(host);

        // Act
        var result = await _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ValueOrDefault);
        Assert.Equal(caCertificate.SubjectName.Name, result.ValueOrDefault.Issuer);
    }

    private static byte[] GenerateCsrBytes(bool isCa)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (isCa)
        {
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        }

        return request.CreateSigningRequest();
    }

    private static byte[] GenerateMinimalValidCsrBytes()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Minimal", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return request.CreateSigningRequest();
    }

    private static byte[] GenerateCsrBytesWithExtensions()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=WithExtensions", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        return request.CreateSigningRequest();
    }

    private static X509Certificate2 GenerateCaCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=CA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var caCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        return new X509Certificate2(caCertificate.Export(X509ContentType.Pfx));
    }
}
