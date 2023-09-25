// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using Moq;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Controllers;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Tests;

public class VersionsControllerTests
{
    private readonly Mock<IComponentUpdater> _mockComponentUpdater;
    private readonly List<IComponentUpdater> _componentUpdaters;
    private readonly VersionsController _controller;

    public VersionsControllerTests()
    {
        _mockComponentUpdater = new Mock<IComponentUpdater>();
        _componentUpdaters = new List<IComponentUpdater> { _mockComponentUpdater.Object };
        _controller = new VersionsController(_componentUpdaters);
    }

    [Fact]
    public async Task GetVersions_ShouldReturnOk_WithComponentVersions()
    {
        // Arrange
        var expectedVersion = new ComponentVersionResponse
        {
            ComponentName = "TestComponent",
            CurrentVersion = new Version("1.0.0")
        };

        _mockComponentUpdater.Setup(updater => updater.GetCurrentVersionAsync())
                             .ReturnsAsync(expectedVersion);

        // Act
        var result = await _controller.GetVersions();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var versions = Assert.IsType<List<ComponentVersionResponse>>(okResult.Value);
        Assert.Single(versions);
        Assert.Equal(expectedVersion.ComponentName, versions[0].ComponentName);
        Assert.Equal(expectedVersion.CurrentVersion, versions[0].CurrentVersion);
    }

    [Fact]
    public async Task GetVersions_ShouldReturnOk_WithNullVersion_WhenUpdaterThrows()
    {
        // Arrange
        _mockComponentUpdater.Setup(updater => updater.GetCurrentVersionAsync())
                             .ThrowsAsync(new Exception("Some error"));

        // Act
        var result = await _controller.GetVersions();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var versions = Assert.IsType<List<ComponentVersionResponse>>(okResult.Value);
        Assert.Single(versions);
        Assert.Null(versions[0].CurrentVersion);
    }
}
