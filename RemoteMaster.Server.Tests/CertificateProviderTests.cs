// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Moq;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Tests;

public class CertificateProviderTests
{
    [Fact]
    public void GetIssuerCertificate_ReturnsCertificateWithPrivateKey()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<CertificateOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new CertificateOptions { CommonName = "TestCA" });

        var certMock = new Mock<ICertificateWrapper>();
        certMock.Setup(c => c.HasPrivateKey).Returns(true);
        var certificates = new List<ICertificateWrapper> { certMock.Object };

        var certificateStoreServiceMock = new Mock<ICertificateStoreService>();
        certificateStoreServiceMock.Setup(s => s.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, "TestCA"))
                                   .Returns(certificates);

        var provider = new CertificateProvider(optionsMock.Object, certificateStoreServiceMock.Object);

        // Act
        var result = provider.GetIssuerCertificate();

        // Assert
        certMock.Verify(c => c.HasPrivateKey, Times.Once);
    }

    [Fact]
    public void GetIssuerCertificate_ThrowsInvalidOperationExceptionWhenCertificateNotFound()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<CertificateOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new CertificateOptions { CommonName = "NonExistentCA" });

        var certificates = new List<ICertificateWrapper>();

        var certificateStoreServiceMock = new Mock<ICertificateStoreService>();
        certificateStoreServiceMock.Setup(s => s.GetCertificates(StoreName.Root, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, "NonExistentCA"))
                                   .Returns(certificates);

        var provider = new CertificateProvider(optionsMock.Object, certificateStoreServiceMock.Object);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetIssuerCertificate());
        Assert.Equal("CA certificate with CommonName 'NonExistentCA' not found.", exception.Message);
    }
}