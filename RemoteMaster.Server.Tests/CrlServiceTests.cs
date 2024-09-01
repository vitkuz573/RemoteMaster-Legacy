// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;
using RemoteMaster.Server.Entities;
using RemoteMaster.Server.Services;
using RemoteMaster.Server.ValueObjects;

namespace RemoteMaster.Server.Tests;

public class CrlServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ICertificateProvider> _certificateProviderMock;
    private readonly MockFileSystem _mockFileSystem;

    public CrlServiceTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<CertificateDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
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

    [Theory]
    [InlineData("1234567890", X509RevocationReason.KeyCompromise)]
    [InlineData("0987654321", X509RevocationReason.CessationOfOperation)]
    [InlineData("1122334455", X509RevocationReason.AffiliationChanged)]
    public async Task RevokeCertificateAsync_RevokesCertificate(string serialNumberValue, X509RevocationReason reason)
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        var serialNumber = SerialNumber.FromExistingValue(serialNumberValue);

        // Act
        var result = await service.RevokeCertificateAsync(serialNumber, reason);

        // Assert
        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);

        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CertificateDbContext>>();
        var context = await contextFactory.CreateDbContextAsync();

        var revokedCertificate = await context.RevokedCertificates
            .FirstOrDefaultAsync(rc => rc.SerialNumber.Value == serialNumber.Value);

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
        var certificateResult = Result.Ok(certificate);
        _certificateProviderMock.Setup(cp => cp.GetIssuerCertificate()).Returns(certificateResult);

        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CertificateDbContext>>();
        var context = await contextFactory.CreateDbContextAsync();

        var crl = new Crl("1");

        var serialNumber = SerialNumber.FromExistingValue("1234567890");
        crl.RevokeCertificate(serialNumber, X509RevocationReason.KeyCompromise);

        context.CertificateRevocationLists.Add(crl);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GenerateCrlAsync();

        // Assert
        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        var crlData = result.ValueOrDefault;
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
    public async Task RevokeCertificateAsync_DuplicateRevocation_ReturnsError()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        // Arrange
        var serialNumberString = "1234567890";
        var serialNumber = SerialNumber.FromExistingValue(serialNumberString);
        var reason = X509RevocationReason.KeyCompromise;

        // Act
        var firstResult = await service.RevokeCertificateAsync(serialNumber, reason);
        var secondResult = await service.RevokeCertificateAsync(serialNumber, reason);

        // Assert
        Assert.True(firstResult.IsSuccess, "First revocation should succeed.");
        Assert.False(secondResult.IsSuccess, "Second revocation should fail.");
        Assert.Contains("already revoked", secondResult.Errors.FirstOrDefault()?.Message);
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
