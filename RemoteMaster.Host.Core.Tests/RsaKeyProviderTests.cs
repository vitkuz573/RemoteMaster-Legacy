// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class RsaKeyProviderTests
{
    private readonly MockFileSystem _mockFileSystem = new();
    private readonly Mock<IApplicationPathProvider> _mockApplicationPathProvider = new();
    private readonly Mock<ILogger<RsaKeyProvider>> _mockLogger = new();
    private readonly RsaKeyProvider _provider;
    private readonly string _programDataPath;

    public RsaKeyProviderTests()
    {
        _programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dataDirectory = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host");
        _mockApplicationPathProvider.Setup(x => x.DataDirectory).Returns(dataDirectory);

        _provider = new RsaKeyProvider(_mockFileSystem, _mockApplicationPathProvider.Object, _mockLogger.Object);
    }

    #region Base Functionality

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnNull_WhenPublicKeyFileDoesNotExist()
    {
        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Warning, "Public key file not found at path", Times.Once());
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnValidKey_WhenPublicKeyFileExists()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.NotNull(rsaKey);
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldCacheResult_AfterFirstCall()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        // Act
        var rsaKey1 = await _provider.GetRsaPublicKeyAsync();
        var rsaKey2 = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Same(rsaKey1, rsaKey2);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsEmpty()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData([]));

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Public key file is empty.", Times.Once());
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsInvalid()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var invalidKeyBytes = new byte[] { 0xFF, 0xFF }; // Invalid key format

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(invalidKeyBytes));

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsInaccessible()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData("dummy data")
        {
            Attributes = FileAttributes.ReadOnly
        });

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileContainsPrivateKey()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var privateKeyBytes = GeneratePrivateKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(privateKeyBytes));

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsTooLarge()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var largeData = new byte[10 * 1024 * 1024]; // 10 MB

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(largeData));

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    #endregion

    #region Path Handling

    [Fact]
    public async Task GetRsaPublicKey_ShouldLogWarning_WhenPathDoesNotExist()
    {
        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Warning, "Public key file not found at path", Times.Once());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetRsaPublicKey_ShouldHandleExtraDataInFile()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKeyWithExtraData();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        // Act
        var rsaKey = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.NotNull(rsaKey);
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldWork_WhenCommonApplicationDataIsUnavailable()
    {
        // Arrange
        // Temporarily unset the environment variable
        var originalProgramData = Environment.GetEnvironmentVariable("ProgramData");
        Environment.SetEnvironmentVariable("ProgramData", null);

        // Reset the data directory to simulate unavailability
        _mockApplicationPathProvider.Setup(x => x.DataDirectory).Returns((string)null!);

        try
        {
            // Act
            var rsaKey = await _provider.GetRsaPublicKeyAsync();

            // Assert
            Assert.Null(rsaKey);
            _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
        }
        finally
        {
            // Restore the environment variable
            Environment.SetEnvironmentVariable("ProgramData", originalProgramData);
            _mockApplicationPathProvider.Setup(x => x.DataDirectory)
                .Returns(_mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host"));
        }
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldBeThreadSafe()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        var results = new RSA?[10];
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            results[i] = await _provider.GetRsaPublicKeyAsync();
        });

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var firstResult = results[0];

        Assert.NotNull(firstResult);
        Assert.All(results, rsa => Assert.Same(firstResult, rsa));
    }

    [Fact]
    public async Task GetRsaPublicKey_ShouldNotReloadKey_WhenFileChangesBetweenCalls()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_programDataPath, "RemoteMaster", "Host", "JWT", "public_key.der");

        var initialPublicKeyBytes = GenerateValidRsaPublicKey();
        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(initialPublicKeyBytes));

        // Act
        var rsaKey1 = await _provider.GetRsaPublicKeyAsync();

        // Modify the file after first call
        var newPublicKeyBytes = GenerateValidRsaPublicKey();
        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(newPublicKeyBytes));

        var rsaKey2 = await _provider.GetRsaPublicKeyAsync();

        // Assert
        Assert.Same(rsaKey1, rsaKey2);
    }

    #endregion

    #region Helper Methods

    private static byte[] GenerateValidRsaPublicKey()
    {
        using var rsa = RSA.Create(2048);

        return rsa.ExportRSAPublicKey();
    }

    private static byte[] GenerateValidRsaPublicKeyWithExtraData()
    {
        var publicKey = GenerateValidRsaPublicKey();
        var extraData = new byte[] { 0x00, 0xFF };

        return [.. publicKey, .. extraData];
    }

    private static byte[] GeneratePrivateKey()
    {
        using var rsa = RSA.Create(2048);

        return rsa.ExportRSAPrivateKey();
    }

    #endregion
}
