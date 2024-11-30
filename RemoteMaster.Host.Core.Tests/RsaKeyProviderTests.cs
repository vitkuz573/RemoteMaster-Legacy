// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class RsaKeyProviderTests
{
    private readonly MockFileSystem _mockFileSystem = new();
    private readonly Mock<ILogger<RsaKeyProvider>> _mockLogger = new();

    private RsaKeyProvider CreateProvider() => new(_mockFileSystem, _mockLogger.Object);

    #region Base Functionality

    [Fact]
    public void GetRsaPublicKey_ShouldReturnNull_WhenPublicKeyFileDoesNotExist()
    {
        // Arrange
        var provider = CreateProvider();

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Warning, "Public key file not found at path", Times.Once());
    }

    [Fact]
    public void GetRsaPublicKey_ShouldReturnValidKey_WhenPublicKeyFileExists()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.NotNull(rsaKey);
    }

    [Fact]
    public void GetRsaPublicKey_ShouldCacheResult_AfterFirstCall()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        // Act
        var rsaKey1 = provider.GetRsaPublicKey();
        var rsaKey2 = provider.GetRsaPublicKey();

        // Assert
        Assert.Same(rsaKey1, rsaKey2);
    }

    #endregion

    #region Error Handling

    [Fact]
    public void GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsEmpty()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData([]));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Public key file is empty.", Times.Once());
    }

    [Fact]
    public void GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsInvalid()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var invalidKeyBytes = new byte[] { 0xFF, 0xFF }; // Invalid key format

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(invalidKeyBytes));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    [Fact]
    public void GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsInaccessible()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var provider = new RsaKeyProvider(mockFileSystem.Object, _mockLogger.Object);
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

        // Set up the file system to simulate an inaccessible file
        mockFileSystem.Setup(fs => fs.File.Exists(publicKeyPath)).Returns(true);
        mockFileSystem.Setup(fs => fs.File.ReadAllBytes(publicKeyPath)).Throws(new UnauthorizedAccessException("Access denied"));
        mockFileSystem.Setup(fs => fs.Path.Combine(It.IsAny<string[]>())).Returns((string[] paths) => Path.Combine(paths));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    [Fact]
    public void GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileContainsPrivateKey()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var privateKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var privateKeyBytes = GeneratePrivateKey();

        _mockFileSystem.AddFile(privateKeyPath, new MockFileData(privateKeyBytes));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    [Fact]
    public void GetRsaPublicKey_ShouldReturnNull_AndLogError_WhenFileIsTooLarge()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var largeData = new byte[10 * 1024 * 1024]; // 10 MB

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(largeData));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to load RSA public key.", Times.Once());
    }

    #endregion

    #region Path Handling

    [Fact]
    public void GetRsaPublicKey_ShouldLogWarning_WhenPathIsInvalid()
    {
        // Arrange
        var provider = CreateProvider();
        var invalidPath = Path.Combine("InvalidPath", "public_key.der");

        _mockFileSystem.AddFile(invalidPath, new MockFileData([0x30, 0x82, 0x01, 0x0A]));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.Null(rsaKey);
        _mockLogger.VerifyLog(LogLevel.Warning, "Public key file not found at path", Times.Once());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetRsaPublicKey_ShouldHandleExtraDataInFile()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKeyWithExtraData();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        // Act
        var rsaKey = provider.GetRsaPublicKey();

        // Assert
        Assert.NotNull(rsaKey);
    }

    [Fact]
    public void GetRsaPublicKey_ShouldWork_WhenCommonApplicationDataIsUnavailable()
    {
        // Arrange
        var provider = CreateProvider();

        // Temporarily unset the environment variable
        var originalProgramData = Environment.GetEnvironmentVariable("ProgramData");
        Environment.SetEnvironmentVariable("ProgramData", null);

        try
        {
            // Act
            var rsaKey = provider.GetRsaPublicKey();

            // Assert
            Assert.Null(rsaKey);
            _mockLogger.VerifyLog(LogLevel.Warning, "Public key file not found at path", Times.Once());
        }
        finally
        {
            // Restore the environment variable
            Environment.SetEnvironmentVariable("ProgramData", originalProgramData);
        }
    }

    [Fact]
    public void GetRsaPublicKey_ShouldBeThreadSafe()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");
        var publicKeyBytes = GenerateValidRsaPublicKey();

        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(publicKeyBytes));

        var results = new RSA[10];

        // Act
        Parallel.For(0, 10, i =>
        {
            results[i] = provider.GetRsaPublicKey();
        });

        // Assert
        var firstResult = results[0];
        Assert.All(results, rsa => Assert.Same(firstResult, rsa));
    }

    [Fact]
    public void GetRsaPublicKey_ShouldNotReloadKey_WhenFileChangesBetweenCalls()
    {
        // Arrange
        var provider = CreateProvider();
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var publicKeyPath = Path.Combine(programDataPath, "RemoteMaster", "Security", "JWT", "public_key.der");

        var initialPublicKeyBytes = GenerateValidRsaPublicKey();
        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(initialPublicKeyBytes));

        // Act
        var rsaKey1 = provider.GetRsaPublicKey();

        // Modify the file after first call
        var newPublicKeyBytes = GenerateValidRsaPublicKey();
        _mockFileSystem.AddFile(publicKeyPath, new MockFileData(newPublicKeyBytes));

        var rsaKey2 = provider.GetRsaPublicKey();

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