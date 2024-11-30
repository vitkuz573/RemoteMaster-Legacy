// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class SyncIndicatorServiceTests
{
    private readonly MockFileSystem _mockFileSystem;
    private readonly Mock<ILogger<SyncIndicatorService>> _mockLogger;
    private readonly SyncIndicatorService _service;

    public SyncIndicatorServiceTests()
    {
        var processPath = Environment.ProcessPath ?? @"C:\FakePath\RemoteMaster.exe";

        _mockFileSystem = new MockFileSystem();
        _mockLogger = new Mock<ILogger<SyncIndicatorService>>();

        var processDirectory = _mockFileSystem.Path.GetDirectoryName(processPath)!;
        _mockFileSystem.AddDirectory(processDirectory);

        _service = new SyncIndicatorService(_mockFileSystem, _mockLogger.Object);
    }

    [Fact]
    public void IsSyncRequired_ReturnsFalse_WhenFileDoesNotExist()
    {
        // Act
        var result = _service.IsSyncRequired();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSyncRequired_ReturnsTrue_WhenFileExists()
    {
        // Arrange
        var syncFilePath = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");
        _mockFileSystem.AddFile(syncFilePath, new MockFileData("Sync required"));

        // Act
        var result = _service.IsSyncRequired();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SetSyncRequired_CreatesFileWithCorrectContent()
    {
        // Act
        _service.SetSyncRequired();

        // Assert
        var syncFilePath = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");
        Assert.True(_mockFileSystem.File.Exists(syncFilePath));
        Assert.Equal("Sync required", _mockFileSystem.File.ReadAllText(syncFilePath));
    }

    [Fact]
    public void SetSyncRequired_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var syncFilePath = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");
        _mockFileSystem.AddFile(syncFilePath, new MockFileData("Existing file is read-only"));
        _mockFileSystem.File.SetAttributes(syncFilePath, FileAttributes.ReadOnly);

        // Act
        _service.SetSyncRequired();

        // Assert
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to create sync indicator file.", Times.Once());
    }

    [Fact]
    public void ClearSyncIndicator_DeletesFile_WhenFileExists()
    {
        // Arrange
        var syncFilePath = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");
        _mockFileSystem.AddFile(syncFilePath, new MockFileData("Sync required"));

        // Act
        _service.ClearSyncIndicator();

        // Assert
        Assert.False(_mockFileSystem.File.Exists(syncFilePath));
    }

    [Fact]
    public void ClearSyncIndicator_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var syncFilePath = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetDirectoryName(Environment.ProcessPath)!, "sync_required.ind");
        _mockFileSystem.AddFile(syncFilePath, new MockFileData("Sync required"));
        _mockFileSystem.File.SetAttributes(syncFilePath, FileAttributes.ReadOnly);

        // Act
        _service.ClearSyncIndicator();

        // Assert
        _mockLogger.VerifyLog(LogLevel.Error, "Failed to delete sync indicator file.", Times.Once());
    }
}
