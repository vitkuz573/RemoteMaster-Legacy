// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.Options;
using Moq;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Tests;

public class CertificateProviderTests
{
    [Fact]
    public void GetIssuerCertificate_ReturnsCertificateWithPrivateKey()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<InternalCertificateOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new InternalCertificateOptions { CommonName = "TestCA" });

        using var certificate = CreateTestCertificateWithPrivateKey();
        var certWrapper = new CertificateWrapper(certificate);

        var certificates = new List<ICertificateWrapper> { certWrapper };

        var certificateStoreServiceMock = new Mock<ICertificateStoreService>();
        certificateStoreServiceMock.Setup(s => s.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, "TestCA"))
                                   .Returns(certificates);

        var provider = new CertificateProvider(optionsMock.Object, certificateStoreServiceMock.Object);

        // Act
        var result = provider.GetIssuerCertificate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ValueOrDefault);
        Assert.True(result.ValueOrDefault.HasPrivateKey);
    }

    [Fact]
    public void GetIssuerCertificate_ReturnsFailureResultWhenCertificateNotFound()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<InternalCertificateOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new InternalCertificateOptions { CommonName = "NonExistentCA" });

        var certificateStoreServiceMock = new Mock<ICertificateStoreService>();
        certificateStoreServiceMock.Setup(s => s.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, "NonExistentCA"))
                                   .Returns([]);

        var provider = new CertificateProvider(optionsMock.Object, certificateStoreServiceMock.Object);

        // Act
        var result = provider.GetIssuerCertificate();

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.Equal($"CA certificate with CommonName 'NonExistentCA' not found.", errorDetails.Message);
    }

    [Fact]
    public void GetIssuerCertificate_ReturnsFailureResultOnException()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<InternalCertificateOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new InternalCertificateOptions { CommonName = "TestCA" });

        var certificateStoreServiceMock = new Mock<ICertificateStoreService>();
        certificateStoreServiceMock.Setup(s => s.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, "TestCA"))
                                   .Throws(new Exception("Test exception"));

        var provider = new CertificateProvider(optionsMock.Object, certificateStoreServiceMock.Object);

        // Act
        var result = provider.GetIssuerCertificate();

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.Equal("Error while retrieving CA certificate.", errorDetails.Message);

        // Check if exception is included in Reasons
        var exceptionError = errorDetails.Reasons.OfType<ExceptionalError>().FirstOrDefault();
        Assert.NotNull(exceptionError);
        Assert.Contains("Test exception", exceptionError.Exception.Message);
    }

    private static X509Certificate2 CreateTestCertificateWithPrivateKey()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("cn=TestCertificate", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
