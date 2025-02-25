// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Tests;

public class FileManagerServiceTests
{
    private readonly MockFileSystem _mockFileSystem;
    private readonly FileManagerService _fileManagerService;

    public FileManagerServiceTests()
    {
        _mockFileSystem = new MockFileSystem();
        _fileManagerService = new FileManagerService(_mockFileSystem);
    }

    [Fact]
    public async Task UploadFileAsync_CreatesFileAtPath()
    {
        // Arrange
        var path = _mockFileSystem.Path.GetTempPath();
        var fileName = _mockFileSystem.Path.GetRandomFileName();
        var fileData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        await _fileManagerService.UploadFileAsync(path, fileName, fileData);
        var filePath = _mockFileSystem.Path.Combine(path, fileName);

        // Assert
        Assert.True(_mockFileSystem.File.Exists(filePath));
        var uploadedData = await _mockFileSystem.File.ReadAllBytesAsync(filePath);
        Assert.Equal(fileData, uploadedData);

        _mockFileSystem.File.Delete(filePath);
    }

    [Fact]
    public async Task DownloadFile_ReturnsMemoryStream()
    {
        // Arrange
        var path = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetTempPath(), _mockFileSystem.Path.GetRandomFileName());
        var fileData = new byte[] { 1, 2, 3, 4, 5 };
        await _mockFileSystem.File.WriteAllBytesAsync(path, fileData);

        // Act
        var result = _fileManagerService.DownloadFile(path);

        // Assert
        using (var memoryStream = new MemoryStream(fileData))
        {
            Assert.True(result.Length == memoryStream.Length);

            for (var i = 0; i < memoryStream.Length; i++)
            {
                Assert.Equal(memoryStream.ReadByte(), result.ReadByte());
            }
        }

        // Cleanup
        await result.DisposeAsync();
        _mockFileSystem.File.Delete(path);
    }

    [Fact]
    public async Task GetFilesAndDirectories_ReturnsCorrectItems()
    {
        // Arrange
        var tempDir = _mockFileSystem.Path.Combine(_mockFileSystem.Path.GetTempPath(), _mockFileSystem.Path.GetRandomFileName());
        _mockFileSystem.Directory.CreateDirectory(tempDir);

        var tempFile = _mockFileSystem.Path.Combine(tempDir, $"{_mockFileSystem.Path.GetRandomFileName()}.txt");
        await _mockFileSystem.File.WriteAllTextAsync(tempFile, "Test file content");

        var subDir = _mockFileSystem.Path.Combine(tempDir, _mockFileSystem.Path.GetRandomFileName());
        _mockFileSystem.Directory.CreateDirectory(subDir);

        // Act
        var result = _fileManagerService.GetFilesAndDirectories(tempDir);

        // Assert
        Assert.Contains(result, item => item is { Name: "..", Type: FileSystemItemType.Directory });
        Assert.Contains(result, item => item.Name == _mockFileSystem.Path.GetFileName(tempFile) && item.Type == FileSystemItemType.File);
        Assert.Contains(result, item => item.Name == _mockFileSystem.Path.GetFileName(subDir) && item.Type == FileSystemItemType.Directory);

        // Cleanup
        _mockFileSystem.File.Delete(tempFile);
        _mockFileSystem.Directory.Delete(subDir);
        _mockFileSystem.Directory.Delete(tempDir);
    }

    [Fact]
    public async Task GetAvailableDrivesAsync_ReturnsDrives()
    {
        // Act
        var result = await _fileManagerService.GetAvailableDrivesAsync();

        // Assert
        var drives = _mockFileSystem.DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new FileSystemItem(d.Name, FileSystemItemType.Drive, 0))
            .ToList();

        Assert.Equal(drives, result);
    }
}
