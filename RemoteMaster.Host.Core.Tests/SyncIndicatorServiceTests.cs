// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class SyncIndicatorServiceTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ILogger<SyncIndicatorService>> _loggerMock;
    private readonly SyncIndicatorService _syncIndicatorService;

    private const string SyncIndicatorFilePath = "C:\\app\\sync_required.ind";

    public SyncIndicatorServiceTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _loggerMock = new Mock<ILogger<SyncIndicatorService>>();

        _fileSystemMock
            .Setup(fs => fs.Path.Combine(It.IsAny<string>(), "sync_required.ind"))
            .Returns(SyncIndicatorFilePath);

        _syncIndicatorService = new SyncIndicatorService(_fileSystemMock.Object, _loggerMock.Object);
    }

    #region IsSyncRequired Tests

    [Fact]
    public void IsSyncRequired_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.File.Exists(SyncIndicatorFilePath)).Returns(true);

        // Act
        var result = _syncIndicatorService.IsSyncRequired();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSyncRequired_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.File.Exists(SyncIndicatorFilePath)).Returns(false);

        // Act
        var result = _syncIndicatorService.IsSyncRequired();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region SetSyncRequired Tests

    [Fact]
    public void SetSyncRequired_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        _fileSystemMock
            .Setup(fs => fs.File.WriteAllText(SyncIndicatorFilePath, "Sync required"))
            .Throws(new IOException("Disk full"));

        // Act
        _syncIndicatorService.SetSyncRequired();

        // Assert
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to create sync indicator file.", Times.Once());
    }

    #endregion

    #region ClearSyncIndicator Tests

    [Fact]
    public void ClearSyncIndicator_ShouldDeleteFile_WhenFileExists()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.File.Exists(SyncIndicatorFilePath)).Returns(true);

        // Act
        _syncIndicatorService.ClearSyncIndicator();

        // Assert
        _fileSystemMock.Verify(fs => fs.File.Delete(SyncIndicatorFilePath), Times.Once);
    }

    [Fact]
    public void ClearSyncIndicator_ShouldNotDeleteFile_WhenFileDoesNotExist()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.File.Exists(SyncIndicatorFilePath)).Returns(false);

        // Act
        _syncIndicatorService.ClearSyncIndicator();

        // Assert
        _fileSystemMock.Verify(fs => fs.File.Delete(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ClearSyncIndicator_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.File.Exists(SyncIndicatorFilePath)).Returns(true);
        _fileSystemMock.Setup(fs => fs.File.Delete(SyncIndicatorFilePath))
            .Throws(new IOException("Access denied"));

        // Act
        _syncIndicatorService.ClearSyncIndicator();

        // Assert
        _loggerMock.VerifyLog(LogLevel.Error, "Failed to delete sync indicator file.", Times.Once());
    }

    #endregion
}
