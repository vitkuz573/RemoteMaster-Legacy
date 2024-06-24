// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class CertificateRequestServiceTests : IDisposable
{
    private readonly CertificateRequestService _certificateRequestService;
    private RSA _rsaKeyPair;

    public CertificateRequestServiceTests()
    {
        _certificateRequestService = new CertificateRequestService();
    }

    [Fact]
    public void GenerateSigningRequest_ValidParameters_ReturnsCsr()
    {
        // Arrange
        var subjectName = new X500DistinguishedName("CN=Test");
        var ipAddresses = new List<string> { "192.168.0.1", "10.0.0.1" };

        // Act
        var csr = _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses, out _rsaKeyPair);

        // Assert
        Assert.NotNull(csr);
        Assert.NotNull(_rsaKeyPair);
        Assert.Contains(csr.CertificateExtensions, ext => ext.Oid.Value == "2.5.29.17"); // SAN extension
        Assert.Contains(csr.CertificateExtensions, ext => ext.Oid.Value == "2.5.29.37"); // Enhanced key usage extension
    }

    [Fact]
    public void GenerateSigningRequest_NullSubjectName_ThrowsArgumentNullException()
    {
        // Arrange
        X500DistinguishedName subjectName = null;
        var ipAddresses = new List<string> { "192.168.0.1", "10.0.0.1" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses, out _rsaKeyPair));
    }

    [Fact]
    public void GenerateSigningRequest_NullIpAddresses_ThrowsArgumentNullException()
    {
        // Arrange
        var subjectName = new X500DistinguishedName("CN=Test");
        List<string> ipAddresses = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses, out _rsaKeyPair));
    }

    [Fact]
    public void GenerateSigningRequest_EmptyIpAddresses_DoesNotThrowException()
    {
        // Arrange
        var subjectName = new X500DistinguishedName("CN=Test");
        var ipAddresses = new List<string>();

        // Act
        var csr = _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses, out _rsaKeyPair);

        // Assert
        Assert.NotNull(csr);
        Assert.NotNull(_rsaKeyPair);
        Assert.Contains(csr.CertificateExtensions, ext => ext.Oid.Value == "2.5.29.17"); // SAN extension
    }

    public void Dispose()
    {
        _rsaKeyPair?.Dispose();
    }
}