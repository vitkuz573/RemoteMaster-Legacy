// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class FileManagerService(IFileSystem fileSystem) : IFileManagerService
{
    public async Task UploadFileAsync(string path, string fileName, byte[] fileData)
    {
        var filePath = fileSystem.Path.Combine(path, fileName);

        if (!fileSystem.Directory.Exists(path))
        {
            fileSystem.Directory.CreateDirectory(path);
        }

        await fileSystem.File.WriteAllBytesAsync(filePath, fileData);
    }

    public Stream DownloadFile(string path)
    {
        var memoryStream = new MemoryStream();

        using (var stream = fileSystem.FileStream.New(path, FileMode.Open))
        {
            stream.CopyTo(memoryStream);
        }

        memoryStream.Position = 0;

        return memoryStream;
    }

    public List<FileSystemItem> GetFilesAndDirectories(string path)
    {
        var items = new List<FileSystemItem>();
        var directoryInfo = fileSystem.DirectoryInfo.New(path);

        if (fileSystem.Path.GetPathRoot(path) == path || directoryInfo.Parent != null)
        {
            items.Add(new FileSystemItem("..", FileSystemItemType.Directory, 0));
        }

        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        items.AddRange(directoryInfo.GetFiles("*", enumerationOptions).Select(file => new FileSystemItem(file.Name, FileSystemItemType.File, file.Length)));
        items.AddRange(directoryInfo.GetDirectories("*", enumerationOptions).Select(directory => new FileSystemItem(directory.Name, FileSystemItemType.Directory, 0)));

        return items;
    }

    public Task<List<FileSystemItem>> GetAvailableDrivesAsync()
    {
        var drives = fileSystem.DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => new FileSystemItem(d.Name, FileSystemItemType.Drive, 0))
            .ToList();

        return Task.FromResult(drives);
    }
}
