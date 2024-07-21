// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Models;
using Serilog;
using Xunit.Abstractions;

namespace RemoteMaster.Server.Tests;

public class CrlServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ICertificateProvider> _certificateProviderMock;
    private readonly MockFileSystem _mockFileSystem;

    public CrlServiceTests(ITestOutputHelper output)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.TestOutput(output)
            .CreateLogger();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<CertificateDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        serviceCollection.AddScoped<IDbContextFactory<CertificateDbContext>, DbContextFactory<CertificateDbContext>>();

        _certificateProviderMock = new Mock<ICertificateProvider>();
        _mockFileSystem = new MockFileSystem();

        serviceCollection.AddSingleton(_certificateProviderMock.Object);
        serviceCollection.AddSingleton<IFileSystem>(_mockFileSystem);
        serviceCollection.AddScoped<ICrlService, CrlService>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    private IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    [Fact]
    public async Task RevokeCertificateAsync_RevokesCertificate()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        // Arrange
        const string serialNumber = "1234567890";
        const X509RevocationReason reason = X509RevocationReason.KeyCompromise;

        // Act
        Log.Information("RevokeCertificateAsync: Revoking certificate with serial number {SerialNumber}", serialNumber);
        var result = await service.RevokeCertificateAsync(serialNumber, reason);
        Log.Information("RevokeCertificateAsync: Certificate with serial number {SerialNumber} revoked", serialNumber);

        // Assert
        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);

        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CertificateDbContext>>();
        var context = await contextFactory.CreateDbContextAsync();
        var revokedCertificate = await context.RevokedCertificates.FirstOrDefaultAsync(rc => rc.SerialNumber == serialNumber);
        Log.Information("RevokeCertificateAsync: Checking if certificate with serial number {SerialNumber} is revoked", serialNumber);

        Assert.NotNull(revokedCertificate);
        Assert.Equal(reason, revokedCertificate.Reason);
    }

    [Fact]
    public async Task GenerateCrlAsync_GeneratesCrl()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        // Arrange
        using var certificate = CreateTestCertificateWithBasicConstraints();
        var certificateResult = Result<X509Certificate2>.Success(certificate);
        _certificateProviderMock.Setup(cp => cp.GetIssuerCertificate()).Returns(certificateResult);

        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CertificateDbContext>>();
        var context = await contextFactory.CreateDbContextAsync();
        var revokedCertificate = new RevokedCertificate
        {
            SerialNumber = "1234567890",
            Reason = X509RevocationReason.KeyCompromise,
            RevocationDate = DateTimeOffset.UtcNow
        };
        context.RevokedCertificates.Add(revokedCertificate);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GenerateCrlAsync();

        // Assert
        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        var crlData = result.Value;
        Assert.NotNull(crlData);
        Assert.NotEmpty(crlData);
    }

    [Fact]
    public async Task PublishCrlAsync_PublishesCrl()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        // Arrange
        var crlData = new byte[] { 0x01, 0x02, 0x03 };
        var crlFilePath = _mockFileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "list.crl");

        // Act
        var result = await service.PublishCrlAsync(crlData);

        // Assert
        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        Assert.True(_mockFileSystem.File.Exists(crlFilePath));
        Assert.Equal(crlData, await _mockFileSystem.File.ReadAllBytesAsync(crlFilePath));
    }

    [Fact]
    public async Task GetCrlMetadataAsync_ReturnsMetadata()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        // Arrange
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CertificateDbContext>>();
        var context = await contextFactory.CreateDbContextAsync();
        var crlInfo = new CrlInfo
        {
            CrlNumber = BigInteger.Zero.ToString(),
            NextUpdate = DateTimeOffset.UtcNow.AddDays(30),
            CrlHash = "hash"
        };
        context.CrlInfos.Add(crlInfo);
        context.RevokedCertificates.Add(new RevokedCertificate
        {
            SerialNumber = "1234567890",
            Reason = X509RevocationReason.KeyCompromise,
            RevocationDate = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCrlMetadataAsync();

        // Assert
        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        var metadata = result.Value;
        Assert.NotNull(metadata);
        Assert.Equal(crlInfo.CrlNumber, metadata.CrlInfo.CrlNumber);
        Assert.Equal(1, metadata.RevokedCertificatesCount);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private static X509Certificate2 CreateTestCertificateWithBasicConstraints()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("cn=TestCertificate", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        return certificate;
    }
}

public class DbContextFactory<TContext>(IServiceProvider serviceProvider) : IDbContextFactory<TContext> where TContext : DbContext
{
    public TContext CreateDbContext()
    {
        return serviceProvider.GetRequiredService<TContext>();
    }

    public Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }
}
