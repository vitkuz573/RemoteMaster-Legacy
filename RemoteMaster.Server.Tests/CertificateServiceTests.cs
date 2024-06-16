// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class CertificateServiceTests
{
    private readonly Mock<ICaCertificateService> _caCertificateServiceMock;
    private readonly Mock<IHostInformationService> _hostInformationServiceMock;
    private readonly ICertificateService _certificateService;

    public CertificateServiceTests()
    {
        _caCertificateServiceMock = new Mock<ICaCertificateService>();
        _hostInformationServiceMock = new Mock<IHostInformationService>();

        _certificateService = new CertificateService(
            _hostInformationServiceMock.Object,
            _caCertificateServiceMock.Object
        );
    }

    [Fact]
    public void IssueCertificate_WithNullCsrBytes_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _certificateService.IssueCertificate(null));
    }

    [Fact]
    public void IssueCertificate_WithCaCsr_ThrowsInvalidOperationException()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(true);
        using var caCertificate = GenerateCaCertificate();

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _certificateService.IssueCertificate(csrBytes));
        Assert.Equal("CSR for CA certificates are not allowed.", exception.Message);
    }

    [Fact]
    public void IssueCertificate_WithValidCsr_ReturnsCertificate()
    {
        // Arrange
        var csrBytes = GenerateCsrBytes(false);
        using var caCertificate = GenerateCaCertificate();
        var computer = new Computer { Name = "localhost", IpAddress = "127.0.0.1", MacAddress = "00-14-22-01-23-45" };

        _caCertificateServiceMock.Setup(x => x.GetCaCertificate(X509ContentType.Pfx)).Returns(caCertificate);
        _hostInformationServiceMock.Setup(x => x.GetHostInformation()).Returns(computer);

        // Act
        var certificate = _certificateService.IssueCertificate(csrBytes);

        // Assert
        Assert.NotNull(certificate);
        Assert.Equal(caCertificate.SubjectName.Name, certificate.Issuer);
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

    private static X509Certificate2 GenerateCaCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=CA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var caCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        return new X509Certificate2(caCertificate.Export(X509ContentType.Pfx));
    }
}

