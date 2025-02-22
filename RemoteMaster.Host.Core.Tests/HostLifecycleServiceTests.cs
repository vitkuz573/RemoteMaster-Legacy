// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Tests;

public class HostLifecycleServiceTests
{
    private readonly Mock<IApiService> _apiServiceMock;
    private readonly MockFileSystem _fileSystem;
    private readonly Mock<IApplicationPathProvider> _applicationPathProviderMock;
    private readonly Mock<ILogger<HostLifecycleService>> _loggerMock;
    private readonly HostLifecycleService _service;

    public HostLifecycleServiceTests()
    {
        _apiServiceMock = new Mock<IApiService>();
        _fileSystem = new MockFileSystem();
        _applicationPathProviderMock = new Mock<IApplicationPathProvider>();
        _loggerMock = new Mock<ILogger<HostLifecycleService>>();

        _service = new HostLifecycleService(
            _apiServiceMock.Object,
            _fileSystem,
            _applicationPathProviderMock.Object,
            _loggerMock.Object
        );
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_ReturnsTrue()
    {
        // Arrange
        const bool force = true;
        const string dataDirectory = "/data";
        const string jwtDirectory = "/data/JWT";
        var jwtPublicKey = new byte[] { 1, 2, 3, 4 };

        _applicationPathProviderMock
            .Setup(p => p.DataDirectory)
            .Returns(dataDirectory);

        _apiServiceMock
            .Setup(api => api.RegisterHostAsync(force))
            .ReturnsAsync(true);

        _apiServiceMock
            .Setup(api => api.GetJwtPublicKeyAsync())
            .ReturnsAsync(jwtPublicKey);

        // Act
        var result = await _service.RegisterAsync(force);

        // Assert
        Assert.True(result);

        // Verify that the JWT directory was created
        Assert.True(_fileSystem.Directory.Exists(jwtDirectory));

        // Verify that the public key file was written with correct contents
        var publicKeyPath = _fileSystem.Path.Combine(jwtDirectory, "public_key.der");
        Assert.True(_fileSystem.File.Exists(publicKeyPath));
        var savedKey = await _fileSystem.File.ReadAllBytesAsync(publicKeyPath);
        Assert.Equal(jwtPublicKey, savedKey);

        // Verify that appropriate log messages were emitted
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Attempting to register host...")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Public key saved successfully at")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Host registration successful with certificate received.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_AlreadyRegistered_ReturnsFalse()
    {
        // Arrange
        const bool force = false;
        const string dataDirectory = "/data";

        _applicationPathProviderMock
            .Setup(p => p.DataDirectory)
            .Returns(dataDirectory);

        _apiServiceMock
            .Setup(api => api.RegisterHostAsync(force))
            .ReturnsAsync(false);

        // Act
        var result = await _service.RegisterAsync(force);

        // Assert
        Assert.False(result);
        _apiServiceMock.Verify(api => api.RegisterHostAsync(force), Times.Once);
        _apiServiceMock.Verify(api => api.GetJwtPublicKeyAsync(), Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Host registration was not successful.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_GetJwtPublicKeyReturnsNull_ReturnsFalseAndLogsError()
    {
        // Arrange
        const bool force = true;
        const string dataDirectory = "/data";

        _applicationPathProviderMock
            .Setup(p => p.DataDirectory)
            .Returns(dataDirectory);

        _apiServiceMock
            .Setup(api => api.RegisterHostAsync(force))
            .ReturnsAsync(true);

        _apiServiceMock
            .Setup(api => api.GetJwtPublicKeyAsync())
            .ReturnsAsync((byte[])null!);

        // Act
        var result = await _service.RegisterAsync(force);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to obtain JWT public key.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsException_LogsErrorAndReturnsFalse()
    {
        // Arrange
        const bool force = true;
        const string dataDirectory = "/data";

        _applicationPathProviderMock
            .Setup(p => p.DataDirectory)
            .Returns(dataDirectory);

        _apiServiceMock
            .Setup(api => api.RegisterHostAsync(force))
            .ThrowsAsync(new Exception("API failure"));

        // Act
        var result = await _service.RegisterAsync(force);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Registering host failed: API failure.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region UnregisterAsync Tests

    [Fact]
    public async Task UnregisterAsync_SuccessfulUnregistration_ReturnsTrue()
    {
        // Arrange
        _apiServiceMock
            .Setup(api => api.UnregisterHostAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _service.UnregisterAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.UnregisterHostAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Host unregister successful.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterAsync_UnregistrationFailed_ReturnsFalse()
    {
        // Arrange
        _apiServiceMock
            .Setup(api => api.UnregisterHostAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _service.UnregisterAsync();

        // Assert
        Assert.False(result);
        _apiServiceMock.Verify(api => api.UnregisterHostAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Host unregister was not successful.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnregisterAsync_ThrowsException_LogsErrorAndReturnsFalse()
    {
        // Arrange
        _apiServiceMock
            .Setup(api => api.UnregisterHostAsync())
            .ThrowsAsync(new Exception("API failure"));

        // Act
        var result = await _service.UnregisterAsync();

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Unregistering host failed: API failure.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region UpdateHostInformationAsync Tests

    [Fact]
    public async Task UpdateHostInformationAsync_Success_ReturnsTrue()
    {
        // Arrange
        _apiServiceMock
            .Setup(api => api.UpdateHostInformationAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateHostInformationAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.UpdateHostInformationAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Host information updated successfully.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHostInformationAsync_Failure_ReturnsFalse()
    {
        // Arrange
        _apiServiceMock
            .Setup(api => api.UpdateHostInformationAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateHostInformationAsync();

        // Assert
        Assert.False(result);
        _apiServiceMock.Verify(api => api.UpdateHostInformationAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Host information update was not successful.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateHostInformationAsync_ThrowsException_LogsErrorAndReturnsFalse()
    {
        // Arrange
        _apiServiceMock
            .Setup(api => api.UpdateHostInformationAsync())
            .ThrowsAsync(new Exception("API failure"));

        // Act
        var result = await _service.UpdateHostInformationAsync();

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Update host information failed: API failure.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region IsHostRegisteredAsync Tests

    [Fact]
    public async Task IsHostRegisteredAsync_Success_ReturnsApiResult()
    {
        // Arrange
        const bool apiResult = true;

        _apiServiceMock
            .Setup(api => api.IsHostRegisteredAsync())
            .ReturnsAsync(apiResult);

        // Act
        var result = await _service.IsHostRegisteredAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.IsHostRegisteredAsync(), Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IsHostRegisteredAsync_NetworkUnreachable_ReturnsTrueAndLogsDebug()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.NetworkUnreachable);
        var httpRequestException = new HttpRequestException("Network error", socketException);

        _apiServiceMock
            .Setup(api => api.IsHostRegisteredAsync())
            .ThrowsAsync(httpRequestException);

        // Act
        var result = await _service.IsHostRegisteredAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.IsHostRegisteredAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Network error (unreachable or connection refused). Assuming host is still registered based on previous state.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task IsHostRegisteredAsync_ConnectionRefused_ReturnsTrueAndLogsDebug()
    {
        // Arrange
        var socketException = new SocketException((int)SocketError.ConnectionRefused);
        var httpRequestException = new HttpRequestException("Connection refused", socketException);

        _apiServiceMock
            .Setup(api => api.IsHostRegisteredAsync())
            .ThrowsAsync(httpRequestException);

        // Act
        var result = await _service.IsHostRegisteredAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.IsHostRegisteredAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Network error (unreachable or connection refused). Assuming host is still registered based on previous state.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task IsHostRegisteredAsync_OtherHttpRequestException_ReturnsTrueAndLogsError()
    {
        // Arrange
        var httpRequestException = new HttpRequestException("Timeout");

        _apiServiceMock
            .Setup(api => api.IsHostRegisteredAsync())
            .ThrowsAsync(httpRequestException);

        // Act
        var result = await _service.IsHostRegisteredAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.IsHostRegisteredAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error checking host registration status: Timeout")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task IsHostRegisteredAsync_OtherException_ReturnsTrueAndLogsError()
    {
        // Arrange
        var exception = new Exception("General failure");

        _apiServiceMock
            .Setup(api => api.IsHostRegisteredAsync())
            .ThrowsAsync(exception);

        // Act
        var result = await _service.IsHostRegisteredAsync();

        // Assert
        Assert.True(result);
        _apiServiceMock.Verify(api => api.IsHostRegisteredAsync(), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error checking host registration status: General failure")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region GetOrganizationAsync Tests

    [Fact]
    public async Task GetOrganizationAddressAsync_Success_ReturnsAddressDto()
    {
        // Arrange
        const string organization = "TestOrg";

        var expectedAddress = new AddressDto("Metropolis", "StateName", "FD");
        var expectedOrganization = new OrganizationDto(It.IsAny<Guid>(), It.IsAny<string>(), expectedAddress);

        _apiServiceMock
            .Setup(api => api.GetOrganizationAsync(organization))
            .ReturnsAsync(expectedOrganization);

        // Act
        var result = await _service.GetOrganizationAddressAsync(organization);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAddress.Locality, result.Locality);
        Assert.Equal(expectedAddress.State, result.State);
        Assert.Equal(expectedAddress.Country, result.Country);

        _apiServiceMock.Verify(api => api.GetOrganizationAsync(organization), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Requesting organization address for organization: {organization}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Successfully retrieved address for organization: {organization}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrganizationAddressAsync_ApiReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange
        const string organization = "TestOrg";

        _apiServiceMock
            .Setup(api => api.GetOrganizationAsync(organization))
            .ReturnsAsync((OrganizationDto)null!);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetOrganizationAddressAsync(organization));

        // Assert
        Assert.Equal($"Failed to retrieve address for organization: {organization}", exception.Message);
        _apiServiceMock.Verify(api => api.GetOrganizationAsync(organization), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Requesting organization address for organization: {organization}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains($"Failed to obtain JWT public key.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetOrganizationAddressAsync_ApiThrowsException_LogsErrorAndThrows()
    {
        // Arrange
        const string organization = "TestOrg";
        var exception = new Exception("API failure");

        _apiServiceMock
            .Setup(api => api.GetOrganizationAsync(organization))
            .ThrowsAsync(exception);

        // Act
        var thrownException = await Assert.ThrowsAsync<Exception>(() => _service.GetOrganizationAddressAsync(organization));

        // Assert
        Assert.Equal("API failure", thrownException.Message);
        _apiServiceMock.Verify(api => api.GetOrganizationAsync(organization), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Error retrieving organization address: API failure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}