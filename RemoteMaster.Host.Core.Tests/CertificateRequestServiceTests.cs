﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class CertificateRequestServiceTests : IDisposable
{
    private readonly CertificateRequestService _certificateRequestService;
    private RSA? _rsaKeyPair;

    public CertificateRequestServiceTests()
    {
        Mock<ILogger<CertificateRequestService>> certificateRequestServiceMock = new();

        _certificateRequestService = new CertificateRequestService(certificateRequestServiceMock.Object);
    }

    [Fact]
    public void GenerateSigningRequest_ValidParameters_ReturnsCsr()
    {
        // Arrange
        var subjectName = new X500DistinguishedName("CN=Test");
        var ipAddresses = new List<IPAddress> { IPAddress.Parse("192.168.0.1"), IPAddress.Parse("10.0.0.1") };

        // Act
        var csr = _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses, out _rsaKeyPair);

        // Assert
        Assert.NotNull(csr);
        Assert.NotNull(_rsaKeyPair);

        // Decode CSR to verify its contents
        var certificateRequest = CertificateRequest.LoadSigningRequest(csr, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, RSASignaturePadding.Pkcs1);
        Assert.Contains(certificateRequest.CertificateExtensions, ext => ext.Oid!.Value == "2.5.29.17"); // SAN extension
        Assert.Contains(certificateRequest.CertificateExtensions, ext => ext.Oid!.Value == "2.5.29.37"); // Enhanced key usage extension
    }

    [Fact]
    public void GenerateSigningRequest_NullSubjectName_ThrowsArgumentNullException()
    {
        // Arrange
        X500DistinguishedName? subjectName = null;
        var ipAddresses = new List<IPAddress> { IPAddress.Parse("192.168.0.1"), IPAddress.Parse("10.0.0.1") };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _certificateRequestService.GenerateSigningRequest(subjectName!, ipAddresses, out _rsaKeyPair));
    }

    [Fact]
    public void GenerateSigningRequest_NullIpAddresses_ThrowsArgumentNullException()
    {
        // Arrange
        var subjectName = new X500DistinguishedName("CN=Test");
        List<IPAddress>? ipAddresses = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses!, out _rsaKeyPair));
    }

    [Fact]
    public void GenerateSigningRequest_EmptyIpAddresses_DoesNotThrowException()
    {
        // Arrange
        var subjectName = new X500DistinguishedName("CN=Test");
        var ipAddresses = new List<IPAddress>();

        // Act
        var csr = _certificateRequestService.GenerateSigningRequest(subjectName, ipAddresses, out _rsaKeyPair);

        // Assert
        Assert.NotNull(csr);
        Assert.NotNull(_rsaKeyPair);

        // Decode CSR to verify its contents
        var certificateRequest = CertificateRequest.LoadSigningRequest(csr, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions, RSASignaturePadding.Pkcs1);
        Assert.Contains(certificateRequest.CertificateExtensions, ext => ext.Oid!.Value == "2.5.29.17"); // SAN extension
    }

    public void Dispose()
    {
        _rsaKeyPair?.Dispose();
    }
}
