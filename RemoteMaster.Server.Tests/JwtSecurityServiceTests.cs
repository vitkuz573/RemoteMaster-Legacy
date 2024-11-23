// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RemoteMaster.Server.Options;
using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Tests;

public class JwtSecurityServiceTests
{
    private readonly JwtOptions _options;
    private readonly MockFileSystem _mockFileSystem;
    private readonly JwtSecurityService _jwtSecurityService;

    public JwtSecurityServiceTests()
    {
        _options = new JwtOptions
        {
            KeysDirectory = "TestKeys",
            KeySize = 2048,
            KeyPassword = "TestPassword"
        };

        Mock<IOptions<JwtOptions>> mockOptions = new();
        mockOptions.Setup(o => o.Value).Returns(_options);

        Mock<ILogger<JwtSecurityService>> mockLogger = new();

        _mockFileSystem = new MockFileSystem();

        _jwtSecurityService = new JwtSecurityService(mockOptions.Object, _mockFileSystem, mockLogger.Object);
    }

    [Fact]
    public async Task EnsureKeysExistAsync_GeneratesKeysWhenNotExist()
    {
        // Arrange
        _mockFileSystem.AddDirectory(_options.KeysDirectory);

        // Act
        var result = await _jwtSecurityService.EnsureKeysExistAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(_mockFileSystem.FileExists(_mockFileSystem.Path.Combine(_options.KeysDirectory, "private_key.der")));
        Assert.True(_mockFileSystem.FileExists(_mockFileSystem.Path.Combine(_options.KeysDirectory, "public_key.der")));
    }

    [Fact]
    public async Task EnsureKeysExistAsync_DoesNotGenerateKeysWhenExist()
    {
        // Arrange
        _mockFileSystem.AddDirectory(_options.KeysDirectory);
        _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(_options.KeysDirectory, "private_key.der"), new MockFileData("dummy"));
        _mockFileSystem.AddFile(_mockFileSystem.Path.Combine(_options.KeysDirectory, "public_key.der"), new MockFileData("dummy"));

        // Act
        var result = await _jwtSecurityService.EnsureKeysExistAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("dummy", await _mockFileSystem.File.ReadAllTextAsync(_mockFileSystem.Path.Combine(_options.KeysDirectory, "private_key.der")));
        Assert.Equal("dummy", await _mockFileSystem.File.ReadAllTextAsync(_mockFileSystem.Path.Combine(_options.KeysDirectory, "public_key.der")));
    }

    [Fact]
    public async Task GetPublicKeyAsync_ReturnsPublicKeyWhenExists()
    {
        // Arrange
        var publicKeyPath = _mockFileSystem.Path.Combine(_options.KeysDirectory, "public_key.der");
        _mockFileSystem.AddFile(publicKeyPath, new MockFileData("publickey"));

        // Act
        var result = await _jwtSecurityService.GetPublicKeyAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("publickey", Encoding.UTF8.GetString(result.Value!));
    }

    [Fact]
    public async Task GetPublicKeyAsync_ReturnsFailureWhenPublicKeyDoesNotExist()
    {
        // Act
        var result = await _jwtSecurityService.GetPublicKeyAsync();

        // Assert
        Assert.False(result.IsSuccess);
        var errorDetails = result.Errors.FirstOrDefault();
        Assert.NotNull(errorDetails);
        Assert.Equal("Public key file does not exist.", errorDetails.Message);
    }
}
