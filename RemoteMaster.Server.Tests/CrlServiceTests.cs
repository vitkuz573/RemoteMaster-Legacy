// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CrlAggregate;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class CrlServiceTests
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ICrlRepository> _crlRepositoryMock;
    private readonly Mock<ICertificateProvider> _certificateProviderMock;
    private readonly MockFileSystem _mockFileSystem;

    public CrlServiceTests()
    {
        var serviceCollection = new ServiceCollection();

        _crlRepositoryMock = new Mock<ICrlRepository>();
        _certificateProviderMock = new Mock<ICertificateProvider>();
        _mockFileSystem = new MockFileSystem();

        serviceCollection.AddSingleton(_certificateProviderMock.Object);
        serviceCollection.AddSingleton(_crlRepositoryMock.Object);
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
        var crl = new Crl(BigInteger.Zero.ToString());

        _crlRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Crl> { crl });

        var result = await service.RevokeCertificateAsync(serialNumber, reason);

        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        Assert.Contains(crl.RevokedCertificates, rc => rc.SerialNumber.Value == serialNumberValue && rc.Reason == reason);
    }

    [Fact]
    public async Task GenerateCrlAsync_GeneratesCrl()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        using var certificate = CreateTestCertificateWithBasicConstraints();
        var certificateResult = Result.Ok(certificate);
        _certificateProviderMock.Setup(cp => cp.GetIssuerCertificate()).Returns(certificateResult);

        var crl = new Crl("1");
        crl.RevokeCertificate(SerialNumber.FromExistingValue("1234567890"), X509RevocationReason.KeyCompromise);

        _crlRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Crl> { crl });

        var result = await service.GenerateCrlAsync();

        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        Assert.NotNull(result.ValueOrDefault);
        Assert.NotEmpty(result.ValueOrDefault);
    }

    [Fact]
    public async Task PublishCrlAsync_PublishesCrl()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        var crlData = new byte[] { 0x01, 0x02, 0x03 };
        var crlFilePath = _mockFileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RemoteMaster", "list.crl");

        var result = await service.PublishCrlAsync(crlData);

        Assert.True(result.IsSuccess, result.Errors.FirstOrDefault()?.Message);
        Assert.True(_mockFileSystem.File.Exists(crlFilePath));
        Assert.Equal(crlData, await _mockFileSystem.File.ReadAllBytesAsync(crlFilePath));
    }

    [Fact]
    public async Task RevokeCertificateAsync_DuplicateRevocation_ReturnsError()
    {
        using var scope = CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ICrlService>();

        var serialNumber = SerialNumber.FromExistingValue("1234567890");
        var crl = new Crl(BigInteger.Zero.ToString());
        crl.RevokeCertificate(serialNumber, X509RevocationReason.KeyCompromise);

        _crlRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Crl> { crl });

        var result = await service.RevokeCertificateAsync(serialNumber, X509RevocationReason.KeyCompromise);

        Assert.False(result.IsSuccess, "Second revocation should fail.");
        Assert.Contains("already revoked", result.Errors.FirstOrDefault()?.Message);
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
