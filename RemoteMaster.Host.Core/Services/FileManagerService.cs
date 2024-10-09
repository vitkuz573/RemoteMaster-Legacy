// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class FileManagerService : IFileManagerService
{
    public async Task UploadFileAsync(string path, string fileName, byte[] fileData)
    {
        var filePath = Path.Combine(path, fileName);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        await File.WriteAllBytesAsync(filePath, fileData);
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

    public List<FileSystemItem> GetFilesAndDirectories(string path)
    {
        var items = new List<FileSystemItem>();
        var directoryInfo = new DirectoryInfo(path);

        if (directoryInfo.Parent != null)
        {
            items.Add(new FileSystemItem("..", FileSystemItem.FileSystemItemType.Directory, 0));
        }

        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };

        items.AddRange(directoryInfo.GetFiles("*", enumerationOptions).Select(file => new FileSystemItem(file.Name, FileSystemItem.FileSystemItemType.File, file.Length)));
        items.AddRange(directoryInfo.GetDirectories("*", enumerationOptions).Select(directory => new FileSystemItem(directory.Name, FileSystemItem.FileSystemItemType.Directory, 0)));

        return items;
    }

    public Task<List<string>> GetAvailableDrivesAsync()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => d.Name)
            .ToList();

        return Task.FromResult(drives);
    }
}
