// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Forms;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class FileManagerService : IFileManagerService
{
    public Task<FileInfo[]> GetFilesAsync(string path)
    {
        var directory = new DirectoryInfo(path);

        return Task.FromResult(directory.GetFiles());
    }

    public Task<DirectoryInfo[]> GetDirectoriesAsync(string path)
    {
        var directory = new DirectoryInfo(path);

        return Task.FromResult(directory.GetDirectories());
    }

    public async Task UploadFileAsync(string path, IBrowserFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var filePath = Path.Combine(path, file.Name);
        using var stream = file.OpenReadStream();
        using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);
    }

    public Stream DownloadFile(string path)
    {
        var memoryStream = new MemoryStream();

        using (var stream = new FileStream(path, FileMode.Open))
        {
            stream.CopyTo(memoryStream);
        }

        memoryStream.Position = 0;

        return memoryStream;
    }

    public async Task<List<FileSystemItem>> GetFilesAndDirectoriesAsync(string path)
    {
        var items = new List<FileSystemItem>();
        var directoryInfo = new DirectoryInfo(path);

        if (directoryInfo.Parent != null)
        {
            items.Add(new FileSystemItem
            {
                Name = "..",
                Type = FileSystemItem.FileSystemItemType.Directory,
                Size = 0
            });
        }

        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        foreach (var file in directoryInfo.GetFiles("*", enumerationOptions))
        {
            items.Add(new FileSystemItem
            {
                Name = file.Name,
                Type = FileSystemItem.FileSystemItemType.File,
                Size = file.Length
            });
        }

        foreach (var directory in directoryInfo.GetDirectories("*", enumerationOptions))
        {
            items.Add(new FileSystemItem
            {
                Name = directory.Name,
                Type = FileSystemItem.FileSystemItemType.Directory,
                Size = 0
            });
        }

        return items;
    }
}
