// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class FileServiceTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _fileSystem = new MockFileSystem();
        _fileService = new FileService(_fileSystem);
    }

    #region CalculateChecksum Tests

    [Fact]
    public void CalculateChecksum_ShouldReturnCorrectChecksum_WhenFileExists()
    {
        const string filePath = "/test.txt";
        const string fileData = "Hello, world!";
        _fileSystem.AddFile(filePath, new MockFileData(fileData));
        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fileData)));

        var checksum = _fileService.CalculateChecksum(filePath);

        Assert.Equal(expectedHash, checksum);
    }

    [Fact]
    public void CalculateChecksum_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        const string filePath = "/nonexistent.txt";
        Assert.Throws<FileNotFoundException>(() => _fileService.CalculateChecksum(filePath));
    }

    #endregion

    #region DeleteFile Tests

    [Fact]
    public void DeleteFile_ShouldDeleteFile_WhenFileExists()
    {
        const string filePath = "/test.txt";
        _fileSystem.AddFile(filePath, new MockFileData("Content"));

        _fileService.DeleteFile(filePath);

        Assert.False(_fileSystem.File.Exists(filePath));
    }

    [Fact]
    public void DeleteFile_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        const string filePath = "/nonexistent.txt";
        Assert.Throws<FileNotFoundException>(() => _fileService.DeleteFile(filePath));
    }

    #endregion

    #region DeleteDirectory Tests

    [Fact]
    public void DeleteDirectory_ShouldDeleteDirectoryRecursively_WhenDirectoryExists()
    {
        const string dirPath = "/testDir";
        _fileSystem.AddDirectory(dirPath);
        _fileSystem.AddFile($"{dirPath}/file.txt", new MockFileData("Content"));

        _fileService.DeleteDirectory(dirPath);

        Assert.False(_fileSystem.Directory.Exists(dirPath));
    }

    [Fact]
    public void DeleteDirectory_ShouldThrowDirectoryNotFoundException_WhenDirectoryDoesNotExist()
    {
        const string dirPath = "/nonexistentDir";
        Assert.Throws<DirectoryNotFoundException>(() => _fileService.DeleteDirectory(dirPath));
    }

    #endregion

    #region CreateDirectory Tests

    [Fact]
    public void CreateDirectory_ShouldCreateDirectory_WhenItDoesNotExist()
    {
        const string dirPath = "/newDir";

        _fileService.CreateDirectory(dirPath);

        Assert.True(_fileSystem.Directory.Exists(dirPath));
    }

    [Fact]
    public void CreateDirectory_ShouldThrowIOException_WhenDirectoryAlreadyExists()
    {
        const string dirPath = "/existingDir";
        _fileSystem.AddDirectory(dirPath);

        Assert.Throws<IOException>(() => _fileService.CreateDirectory(dirPath));
    }

    #endregion

    #region CopyFile Tests

    [Fact]
    public void CopyFile_ShouldCopyFile_WhenSourceFileExists()
    {
        const string sourceFile = "/source.txt";
        const string destFile = "/dest.txt";
        _fileSystem.AddFile(sourceFile, new MockFileData("Content"));

        _fileService.CopyFile(sourceFile, destFile);

        Assert.True(_fileSystem.File.Exists(destFile));
        Assert.Equal("Content", _fileSystem.File.ReadAllText(destFile));
    }

    [Fact]
    public void CopyFile_ShouldThrowFileNotFoundException_WhenSourceFileDoesNotExist()
    {
        const string sourceFile = "/nonexistent.txt";
        const string destFile = "/dest.txt";
        Assert.Throws<FileNotFoundException>(() => _fileService.CopyFile(sourceFile, destFile));
    }

    #endregion

    #region CopyDirectory Tests

    [Fact]
    public void CopyDirectory_ShouldCopyDirectoryRecursively_WhenSourceDirectoryExists()
    {
        const string sourceDir = "/sourceDir";
        const string destDir = "/destDir";
        _fileSystem.AddDirectory(sourceDir);
        _fileSystem.AddFile($"{sourceDir}/file.txt", new MockFileData("Content"));

        _fileService.CopyDirectory(sourceDir, destDir);

        Assert.True(_fileSystem.Directory.Exists(destDir));
        Assert.True(_fileSystem.File.Exists($"{destDir}/file.txt"));
        Assert.Equal("Content", _fileSystem.File.ReadAllText($"{destDir}/file.txt"));
    }

    [Fact]
    public void CopyDirectory_ShouldThrowDirectoryNotFoundException_WhenSourceDirectoryDoesNotExist()
    {
        const string sourceDir = "/nonexistentDir";
        const string destDir = "/destDir";
        Assert.Throws<DirectoryNotFoundException>(() => _fileService.CopyDirectory(sourceDir, destDir));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CopyFile_ShouldOverwriteFile_WhenOverwriteIsTrue()
    {
        const string sourceFile = "/source.txt";
        const string destFile = "/dest.txt";
        _fileSystem.AddFile(sourceFile, new MockFileData("New Content"));
        _fileSystem.AddFile(destFile, new MockFileData("Old Content"));

        _fileService.CopyFile(sourceFile, destFile, overwrite: true);

        Assert.Equal("New Content", _fileSystem.File.ReadAllText(destFile));
    }

    [Fact]
    public void CopyFile_ShouldNotOverwriteFile_WhenOverwriteIsFalse()
    {
        const string sourceFile = "/source.txt";
        const string destFile = "/dest.txt";
        _fileSystem.AddFile(sourceFile, new MockFileData("New Content"));
        _fileSystem.AddFile(destFile, new MockFileData("Old Content"));

        try
        {
            _fileService.CopyFile(sourceFile, destFile, overwrite: false);
        }
        catch (IOException)
        {
            // Expected exception since overwrite is false and file exists.
        }

        Assert.Equal("Old Content", _fileSystem.File.ReadAllText(destFile));
    }

    [Fact]
    public void CopyDirectory_ShouldCopySubDirectoriesRecursively()
    {
        const string sourceDir = "/sourceDir";
        const string destDir = "/destDir";
        const string nestedDir = "/sourceDir/subDir";
        const string nestedFile = "/sourceDir/subDir/subFile.txt";
        _fileSystem.AddDirectory(sourceDir);
        _fileSystem.AddFile($"{sourceDir}/file.txt", new MockFileData("Content"));
        _fileSystem.AddDirectory(nestedDir);
        _fileSystem.AddFile(nestedFile, new MockFileData("Sub Content"));

        _fileService.CopyDirectory(sourceDir, destDir);

        Assert.True(_fileSystem.Directory.Exists($"{destDir}/subDir"));
        Assert.Equal("Sub Content", _fileSystem.File.ReadAllText($"{destDir}/subDir/subFile.txt"));
    }

    #endregion
}
