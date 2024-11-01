// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Services;
using RemoteMaster.Shared.Enums;

namespace RemoteMaster.Host.Core.Tests;

public class FileManagerServiceTests
{
    private readonly FileManagerService _fileManagerService = new();

    [Fact]
    public async Task UploadFileAsync_CreatesFileAtPath()
    {
        // Arrange
        var path = Path.GetTempPath();
        var fileName = Guid.NewGuid().ToString();
        var fileData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        await _fileManagerService.UploadFileAsync(path, fileName, fileData);
        var filePath = Path.Combine(path, fileName);

        // Assert
        Assert.True(File.Exists(filePath));
        var uploadedData = await File.ReadAllBytesAsync(filePath);
        Assert.Equal(fileData, uploadedData);

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public void DownloadFile_ReturnsMemoryStream()
    {
        // Arrange
        var path = Path.GetTempFileName();
        var fileData = new byte[] { 1, 2, 3, 4, 5 };
        File.WriteAllBytes(path, fileData);

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
        result.Dispose();
        File.Delete(path);
    }

    [Fact]
    public void GetFilesAndDirectories_ReturnsCorrectItems()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.txt");
        File.WriteAllText(tempFile, "Test file content");

        var subDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(subDir);

        // Act
        var result = _fileManagerService.GetFilesAndDirectories(tempDir);

        // Assert
        Assert.Contains(result, item => item is { Name: "..", Type: FileSystemItemType.Directory });
        Assert.Contains(result, item => item.Name == Path.GetFileName(tempFile) && item.Type == FileSystemItemType.File);
        Assert.Contains(result, item => item.Name == Path.GetFileName(subDir) && item.Type == FileSystemItemType.Directory);

        // Cleanup
        File.Delete(tempFile);
        Directory.Delete(subDir);
        Directory.Delete(tempDir);
    }

    [Fact]
    public async Task GetAvailableDrivesAsync_ReturnsDrives()
    {
        // Act
        var result = await _fileManagerService.GetAvailableDrivesAsync();

        // Assert
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => d.Name)
            .ToList();

        Assert.Equal(drives, result);
    }
}