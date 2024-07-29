// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class CertificateServiceTests
{
    private readonly Mock<ICaCertificateService> _caCertificateServiceMock;
    private readonly Mock<IHostInformationService> _hostInformationServiceMock;
    private readonly Mock<ISerialNumberService> _serialNumberServiceMock;
    private readonly CertificateService _certificateService;

    public CertificateServiceTests()
    {
        _caCertificateServiceMock = new Mock<ICaCertificateService>();
        _hostInformationServiceMock = new Mock<IHostInformationService>();
        _serialNumberServiceMock = new Mock<ISerialNumberService>();

        _certificateService = new CertificateService(
            _hostInformationServiceMock.Object,
            _caCertificateServiceMock.Object,
            _serialNumberServiceMock.Object
        );
    }

    [Fact]
    public void IssueCertificate_WithNullCsrBytes_ReturnsArgumentNullExceptionResult()
    {
        // Act
        var result = _certificateService.IssueCertificate(null!);

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.IsType<ArgumentNullException>(errorDetails.Exception);
    }

    [Fact]
    public void IssueCertificate_WithCaCsr_ReturnsInvalidOperationExceptionResult()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(true);
        using var caCertificate = GenerateCaCertificate();

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);

        // Act
        var result = _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.IsType<InvalidOperationException>(errorDetails.Exception);
        Assert.Equal("CSR for CA certificates are not allowed.", errorDetails.Exception.Message);
    }

    [Fact]
    public void IssueCertificate_WithValidCsr_ReturnsSuccessResult()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(false);
        using var caCertificate = GenerateCaCertificate();
        var computer = new ComputerDto("localhost", "127.0.0.1", "00-14-22-01-23-45");
        var serialNumber = Result<byte[]>.Success([0x01, 0x02, 0x03, 0x04]);

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(computer);
        _serialNumberServiceMock.Setup(x => x.GenerateSerialNumber()).Returns(serialNumber);

        // Act
        var result = _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(caCertificate.SubjectName.Name, result.Value.Issuer);
    }

    [Fact]
    public void IssueCertificate_WithMinimalValidCsr_ReturnsSuccessResult()
    {
        // Arrange
        var csrBytes = GenerateMinimalValidCsrBytes();
        using var caCertificate = GenerateCaCertificate();
        var computer = new ComputerDto("localhost", "127.0.0.1", "00-14-22-01-23-45");
        var serialNumber = Result<byte[]>.Success([0x01, 0x02, 0x03, 0x04]);

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(computer);
        _serialNumberServiceMock.Setup(x => x.GenerateSerialNumber()).Returns(serialNumber);

        // Act
        var result = _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(caCertificate.SubjectName.Name, result.Value.Issuer);
    }

    [Fact]
    public void IssueCertificate_WithCsrContainingExtensions_ReturnsSuccessResult()
    {
        // Arrange
        var csrBytes = GenerateCsrBytesWithExtensions();
        using var caCertificate = GenerateCaCertificate();
        var computer = new ComputerDto("localhost", "127.0.0.1", "00-14-22-01-23-45");
        var serialNumber = Result<byte[]>.Success([0x01, 0x02, 0x03, 0x04]);

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(computer);
        _serialNumberServiceMock.Setup(x => x.GenerateSerialNumber()).Returns(serialNumber);

        // Act
        var result = _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(caCertificate.SubjectName.Name, result.Value.Issuer);
    }

    [Fact]
    public void IssueCertificate_UsesMockedSerialNumbers()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(false);
        using var caCertificate = GenerateCaCertificate();
        var computer = new ComputerDto("localhost", "127.0.0.1", "00-14-22-01-23-45");

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(computer);

        _serialNumberServiceMock.SetupSequence(s => s.GenerateSerialNumber())
            .Returns(Result<byte[]>.Success(Guid.NewGuid().ToByteArray()))
            .Returns(Result<byte[]>.Success(Guid.NewGuid().ToByteArray()));

        var certificateService1 = new CertificateService(
            _hostInformationServiceMock.Object,
            _caCertificateServiceMock.Object,
            _serialNumberServiceMock.Object
        );

        var certificateService2 = new CertificateService(
            _hostInformationServiceMock.Object,
            _caCertificateServiceMock.Object,
            _serialNumberServiceMock.Object
        );

        // Act
        var result1 = certificateService1.IssueCertificate(csrBytes);
        var result2 = certificateService2.IssueCertificate(csrBytes);

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.NotNull(result1.Value);
        Assert.NotNull(result2.Value);
        Assert.NotEqual(result1.Value.SerialNumber, result2.Value.SerialNumber);
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
