﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Security.Cryptography;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class FileService(IFileSystem fileSystem) : IFileService
{
    public string CalculateChecksum(string filePath)
    {
        if (!fileSystem.File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        using var sha256 = SHA256.Create();
        using var stream = fileSystem.File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);

        return Convert.ToHexStringLower(hash);
    }

    public void DeleteFile(string filePath)
    {
        if (!fileSystem.File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        fileSystem.File.Delete(filePath);
    }

    public void DeleteDirectory(string directoryPath, bool recursive = true)
    {
        if (!fileSystem.Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        fileSystem.Directory.Delete(directoryPath, recursive);
    }

    public void CreateDirectory(string directoryPath)
    {
        if (fileSystem.Directory.Exists(directoryPath))
        {
            throw new IOException($"Directory already exists: {directoryPath}");
        }

        fileSystem.Directory.CreateDirectory(directoryPath);
    }

    public void CopyFile(string sourceFile, string destinationFile, bool overwrite = false)
    {
        if (!fileSystem.File.Exists(sourceFile))
        {
            throw new FileNotFoundException($"Source file not found: {sourceFile}");
        }

        var destinationDirectory = fileSystem.Path.GetDirectoryName(destinationFile);

        if (!string.IsNullOrEmpty(destinationDirectory) && !fileSystem.Directory.Exists(destinationDirectory))
        {
            fileSystem.Directory.CreateDirectory(destinationDirectory);
        }

        fileSystem.File.Copy(sourceFile, destinationFile, overwrite);
    }

    public void CopyDirectory(string sourceDirectory, string destinationDirectory, bool overwrite = false)
    {
        if (!fileSystem.Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");
        }

        if (!fileSystem.Directory.Exists(destinationDirectory))
        {
            fileSystem.Directory.CreateDirectory(destinationDirectory);
        }

        foreach (var file in fileSystem.Directory.GetFiles(sourceDirectory))
        {
            var destFile = fileSystem.Path.Combine(destinationDirectory, fileSystem.Path.GetFileName(file));

            CopyFile(file, destFile, overwrite);
        }

        foreach (var subDir in fileSystem.Directory.GetDirectories(sourceDirectory))
        {
            var destSubDir = fileSystem.Path.Combine(destinationDirectory, fileSystem.Path.GetFileName(subDir));

            CopyDirectory(subDir, destSubDir, overwrite);
        }
    }

    public async Task WaitForFileReleaseAsync(string directory, List<string>? excludedFolders = null)
    {
        var locked = true;

        while (locked)
        {
            locked = false;

            foreach (var file in fileSystem.DirectoryInfo.New(directory).GetFiles("*", SearchOption.AllDirectories))
            {
                var directoryName = file.DirectoryName;

                if (directoryName == null || (excludedFolders != null && excludedFolders.Any(directoryName.Contains)))
                {
                    continue;
                }

                try
                {
                    await using var stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    stream.Close();
                }
                catch
                {
                    locked = true;
                    await Task.Delay(2000);
                    break;
                }
            }
        }
    }
}
