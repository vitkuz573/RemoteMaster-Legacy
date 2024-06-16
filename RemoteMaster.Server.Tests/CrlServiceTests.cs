// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class CrlServiceTests : IDisposable
{
    private readonly ICrlService _crlService;
    private readonly CertificateDbContext _context;
    private readonly Mock<IDbContextFactory<CertificateDbContext>> _contextFactoryMock;
    private readonly Mock<ICertificateProvider> _certificateProviderMock;
    private readonly MockFileSystem _fileSystem;

    public CrlServiceTests()
    {
        var options = new DbContextOptionsBuilder<CertificateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CertificateDbContext(options);
        _contextFactoryMock = new Mock<IDbContextFactory<CertificateDbContext>>();
        _contextFactoryMock.Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CertificateDbContext(options));

        _certificateProviderMock = new Mock<ICertificateProvider>();
        _fileSystem = new MockFileSystem();
        _crlService = new CrlService(_contextFactoryMock.Object, _certificateProviderMock.Object, _fileSystem);
    }

    [Fact]
    public async Task RevokeCertificateAsync_RevokesCertificate()
    {
        // Arrange
        var serialNumber = "123456";
        var reason = X509RevocationReason.KeyCompromise;

        // Act
        await _crlService.RevokeCertificateAsync(serialNumber, reason);

        // Assert
        var revokedCert = await _context.RevokedCertificates.FirstOrDefaultAsync(rc => rc.SerialNumber == serialNumber);
        Assert.NotNull(revokedCert);
        Assert.Equal(reason, revokedCert.Reason);
        Assert.True((DateTimeOffset.UtcNow - revokedCert.RevocationDate).TotalSeconds < 10);
    }

    [Fact]
    public async Task RevokeCertificateAsync_LogsIfAlreadyRevoked()
    {
        // Arrange
        var serialNumber = "123456";
        var reason = X509RevocationReason.KeyCompromise;
        _context.RevokedCertificates.Add(new RevokedCertificate { SerialNumber = serialNumber, Reason = reason });
        await _context.SaveChangesAsync();

        // Act
        await _crlService.RevokeCertificateAsync(serialNumber, reason);

        // Assert
        _contextFactoryMock.Verify(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCrlAsync_GeneratesCrl()
    {
        // Arrange
        var issuerCertificate = GenerateTestCertificate();
        _certificateProviderMock.Setup(provider => provider.GetIssuerCertificate()).Returns(issuerCertificate);

        // Act
        var crlData = await _crlService.GenerateCrlAsync();

        // Assert
        Assert.NotNull(crlData);
        Assert.NotEmpty(crlData);
    }

    [Fact]
    public async Task PublishCrlAsync_PublishesCrl()
    {
        // Arrange
        var crlData = new byte[] { 0x01, 0x02, 0x03 };
        var testProgramDataPath = _fileSystem.Path.GetTempPath();
        var crlFilePath = _fileSystem.Path.Combine(testProgramDataPath, "RemoteMaster", "list.crl");

        var directoryPath = _fileSystem.Path.GetDirectoryName(crlFilePath);
        _fileSystem.Directory.CreateDirectory(directoryPath);

        // Act
        var result = await _crlService.PublishCrlAsync(crlData, testProgramDataPath);

        // Assert
        Assert.True(result, "Expected PublishCrlAsync to return true, but it returned false.");
        Assert.True(_fileSystem.File.Exists(crlFilePath), $"Expected file at {crlFilePath} to exist, but it does not.");
        var publishedData = await _fileSystem.File.ReadAllBytesAsync(crlFilePath);
        Assert.Equal(crlData, publishedData);
    }

    [Fact]
    public async Task GetCrlMetadataAsync_ReturnsMetadata()
    {
        // Arrange
        var crlInfo = new CrlInfo { CrlNumber = "1", NextUpdate = DateTimeOffset.UtcNow, CrlHash = "testhash" };
        _context.CrlInfos.Add(crlInfo);
        _context.RevokedCertificates.Add(new RevokedCertificate { SerialNumber = "123456", Reason = X509RevocationReason.KeyCompromise });
        await _context.SaveChangesAsync();

        // Act
        var metadata = await _crlService.GetCrlMetadataAsync();

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(1, metadata.RevokedCertificatesCount);
        Assert.Equal(crlInfo.CrlNumber, metadata.CrlInfo.CrlNumber);
    }

    [Fact]
    public async Task GetCrlMetadataAsync_ThrowsIfMetadataUnavailable()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _crlService.GetCrlMetadataAsync());
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static X509Certificate2 GenerateTestCertificate()
    {
        using var ecdsa = ECDsa.Create();
        var req = new CertificateRequest("CN=Test", ecdsa, HashAlgorithmName.SHA256);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        return req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
    }
}
