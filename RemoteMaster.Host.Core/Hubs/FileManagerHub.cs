// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Hubs;

public class FileManagerHub(IFileSystem fileSystem, IFileManagerService fileManagerService) : Hub<IFileManagerClient>
{
    [Authorize(Policy = "UploadFilePolicy")]
    public async Task UploadFile(FileUploadDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await fileManagerService.UploadFileAsync(dto.DestinationPath, dto.Name, dto.Data);
    }

    [Authorize(Policy = "DownloadFilePolicy")]
    public async Task DownloadFile(string path)
    {
        using var stream = fileManagerService.DownloadFile(path) as MemoryStream ?? throw new InvalidOperationException("Expected a MemoryStream");
        var bytes = stream.ToArray();

        await Clients.Caller.ReceiveFile(bytes, fileSystem.Path.GetFileName(path));
    }

    [Authorize(Policy = "ViewFilesPolicy")]
    public async Task GetFilesAndDirectories(string path)
    {
        var items = fileManagerService.GetFilesAndDirectories(path);

        await Clients.Caller.ReceiveFilesAndDirectories(items);
    }

    [Authorize(Policy = "GetDrivesPolicy")]
    public async Task GetAvailableDrives()
    {
        var drives = await fileManagerService.GetAvailableDrivesAsync();

        await Clients.Caller.ReceiveAvailableDrives(drives);
    }
}
