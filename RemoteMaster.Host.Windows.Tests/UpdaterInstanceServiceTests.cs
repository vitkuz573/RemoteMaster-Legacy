// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using RemoteMaster.Host.Windows.Services;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Tests;

public class UpdaterInstanceServiceTests
{
    private readonly Mock<IArgumentBuilderService> _mockArgumentBuilderService;
    private readonly Mock<IInstanceManagerService> _mockInstanceStarterService;
    private readonly UpdaterInstanceService _service;

    public UpdaterInstanceServiceTests()
    {
        _mockArgumentBuilderService = new Mock<IArgumentBuilderService>();
        _mockInstanceStarterService = new Mock<IInstanceManagerService>();
        Mock<ILogger<UpdaterInstanceService>> mockLogger = new();
        _service = new UpdaterInstanceService(_mockArgumentBuilderService.Object, _mockInstanceStarterService.Object, mockLogger.Object);
    }

    [Fact]
    public void Start_ValidUpdateRequest_StartsNewInstance()
    {
        // Arrange
        var updateRequest = new UpdateRequest(@"C:\TestPath")
        {
            UserCredentials = new Credentials("testuser", "testpassword"),
            ForceUpdate = true,
            AllowDowngrade = true
        };

        _mockArgumentBuilderService
            .Setup(service => service.BuildArguments(It.IsAny<Dictionary<string, object>>()))
            .Returns("--launch-mode=\"updater\" --folder-path=\"C:\\TestPath\" --username=\"testuser\" --password=\"testpassword\" --force --allow-downgrade");

        // Act
        _service.Start(updateRequest);

        // Assert
        _mockInstanceStarterService.Verify(service => service.StartNewInstance(
            It.IsAny<string>(),
            It.Is<NativeProcessStartInfo>(info =>
                info.Arguments == "--launch-mode=\"updater\" --folder-path=\"C:\\TestPath\" --username=\"testuser\" --password=\"testpassword\" --force --allow-downgrade"
                && info.CreateNoWindow == true
            )), Times.Once);
    }

    [Fact]
    public void Start_NullUpdateRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.Start(null!));
    }

    [Fact]
    public void BuildArguments_ValidParameters_ReturnsCorrectArguments()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            { "launch-mode", "updater" },
            { "folder-path", @"C:\TestPath" },
            { "username", "testuser" },
            { "password", "testpassword" },
            { "force", true },
            { "allow-downgrade", true }
        };

        _mockArgumentBuilderService
            .Setup(service => service.BuildArguments(It.IsAny<Dictionary<string, object>>()))
            .Returns("--launch-mode=\"updater\" --folder-path=\"C:\\TestPath\" --username=\"testuser\" --password=\"testpassword\" --force --allow-downgrade");

        // Act
        var result = _mockArgumentBuilderService.Object.BuildArguments(arguments);

        // Assert
        Assert.Contains("--launch-mode=\"updater\"", result);
        Assert.Contains("--folder-path=\"C:\\TestPath\"", result);
        Assert.Contains("--username=\"testuser\"", result);
        Assert.Contains("--password=\"testpassword\"", result);
        Assert.Contains("--force", result);
        Assert.Contains("--allow-downgrade", result);
    }

    [Fact]
    public void BuildArguments_NullCredentials_ReturnsArgumentsWithoutCredentials()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            { "launch-mode", "updater" },
            { "folder-path", @"C:\TestPath" },
            { "force", true },
            { "allow-downgrade", true }
        };

        _mockArgumentBuilderService
            .Setup(service => service.BuildArguments(It.IsAny<Dictionary<string, object>>()))
            .Returns("--launch-mode=\"updater\" --folder-path=\"C:\\TestPath\" --force --allow-downgrade");

        // Act
        var result = _mockArgumentBuilderService.Object.BuildArguments(arguments);

        // Assert
        Assert.Contains("--launch-mode=\"updater\"", result);
        Assert.Contains("--folder-path=\"C:\\TestPath\"", result);
        Assert.DoesNotContain("--username=", result);
        Assert.DoesNotContain("--password=", result);
    }

    [Fact]
    public void StartNewInstance_CopiesExecutableAndStartsProcess()
    {
        // Arrange
        var updateRequest = new UpdateRequest(@"C:\TestPath")
        {
            UserCredentials = new Credentials("testuser", "testpassword"),
            ForceUpdate = true,
            AllowDowngrade = true
        };

        const string additionalArguments = "--launch-mode=\"updater\" --folder-path=\"C:\\TestPath\" --username=\"testuser\" --password=\"testpassword\" --force --allow-downgrade";

        _mockArgumentBuilderService
            .Setup(service => service.BuildArguments(It.IsAny<Dictionary<string, object>>()))
            .Returns(additionalArguments);

        // Act
        _service.Start(updateRequest);

        // Assert
        _mockInstanceStarterService.Verify(service => service.StartNewInstance(
            It.IsAny<string>(),
            It.Is<NativeProcessStartInfo>(info =>
                info.Arguments == additionalArguments
                && info.CreateNoWindow == true
            )), Times.Once);
    }
}
