// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Mvc;
using Moq;
using RemoteMaster.Updater.Abstractions;
using RemoteMaster.Updater.Controllers;
using RemoteMaster.Updater.Models;

namespace RemoteMaster.Updater.Tests;

public class UpdateControllerTests
{
    private readonly Mock<IComponentUpdater> _mockComponentUpdater;
    private readonly List<IComponentUpdater> _componentUpdaters;
    private readonly UpdateController _controller;

    public UpdateControllerTests()
    {
        _mockComponentUpdater = new Mock<IComponentUpdater>();
        _componentUpdaters = new List<IComponentUpdater> { _mockComponentUpdater.Object };
        _controller = new UpdateController(_componentUpdaters);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldReturnBadRequest_WhenUpdateRequestIsNull()
    {
        var result = await _controller.CheckForUpdates(null);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldReturnBadRequest_WhenDecryptionFails()
    {
        var badUpdateRequest = new UpdateRequest
        {
            Login = "bad_encrypted_string",
            Password = "some_password",
            SharedFolder = "some_folder"
        };

        var result = await _controller.CheckForUpdates(badUpdateRequest);

        Assert.IsType<BadRequestObjectResult>(result);
        var badRequestResult = (BadRequestObjectResult)result;
        var errorResponse = Assert.IsType<ErrorResponse>(badRequestResult.Value);
        Assert.Contains("Decryption failed", errorResponse.ErrorMessage);
    }

    // [Fact]
    // public async Task CheckForUpdates_ShouldReturnOk_WithUpdateResponses()
    // {
    //     // Arrange
    //     var goodUpdateRequest = new UpdateRequest
    //     {
    //         Login = "some_encrypted_string",
    //         Password = "some_encrypted_password",
    //         SharedFolder = "some_folder"
    //     };
    // 
    //     var expectedUpdateResponse = new UpdateResponse
    //     {
    //         ComponentName = "TestComponent",
    //         CurrentVersion = new Version("1.0.0"),
    //         IsUpdateAvailable = true
    //     };
    // 
    //     _mockComponentUpdater.Setup(updater => updater.IsUpdateAvailableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    //                          .ReturnsAsync(expectedUpdateResponse);
    // 
    //     // Act
    //     var result = await _controller.CheckForUpdates(goodUpdateRequest);
    // 
    //     // Assert
    //     Assert.IsType<OkObjectResult>(result);
    //     var okResult = (OkObjectResult)result;
    //     var updateResponses = Assert.IsType<List<UpdateResponse>>(okResult.Value);
    //     Assert.Single(updateResponses);
    //     Assert.Equal(expectedUpdateResponse.ComponentName, updateResponses[0].ComponentName);
    //     Assert.Equal(expectedUpdateResponse.CurrentVersion, updateResponses[0].CurrentVersion);
    //     Assert.Equal(expectedUpdateResponse.IsUpdateAvailable, updateResponses[0].IsUpdateAvailable);
    // }

    [Fact]
    public async Task Update_ShouldReturnBadRequest_WhenUpdateRequestIsNull()
    {
        var result = await _controller.Update(null);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // [Fact]
    // public async Task Update_ShouldReturnOk_WithUpdateResponses()
    // {
    //     // Arrange
    //     var goodUpdateRequest = new UpdateRequest
    //     {
    //         Login = "some_encrypted_string",
    //         Password = "some_encrypted_password",
    //         SharedFolder = "some_folder"
    //     };
    // 
    //     var expectedUpdateResponse = new UpdateResponse
    //     {
    //         ComponentName = "TestComponent",
    //         Message = "Update completed successfully.",
    //         CurrentVersion = new Version("1.0.0")
    //     };
    // 
    //     _mockComponentUpdater.Setup(updater => updater.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    //                          .Returns(Task.CompletedTask);
    // 
    //     _mockComponentUpdater.Setup(updater => updater.IsUpdateAvailableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    //                          .ReturnsAsync(new UpdateResponse { IsUpdateAvailable = true });
    // 
    //     // Act
    //     var result = await _controller.Update(goodUpdateRequest);
    // 
    //     // Assert
    //     Assert.IsType<OkObjectResult>(result);
    //     var okResult = (OkObjectResult)result;
    //     var updateResponses = Assert.IsType<List<UpdateResponse>>(okResult.Value);
    //     Assert.Single(updateResponses);
    //     Assert.Equal(expectedUpdateResponse.ComponentName, updateResponses[0].ComponentName);
    //     Assert.Equal(expectedUpdateResponse.Message, updateResponses[0].Message);
    //     Assert.Equal(expectedUpdateResponse.CurrentVersion, updateResponses[0].CurrentVersion);
    // }
}
