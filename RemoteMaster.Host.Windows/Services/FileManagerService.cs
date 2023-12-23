// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Forms;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

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

    public async Task<(FileInfo[], DirectoryInfo[])> GetFilesAndDirectoriesAsync(string path)
    {
        var directory = new DirectoryInfo(path);
        var files = directory.GetFiles();
        var directories = directory.GetDirectories();

        return (files, directories);
    }
}
