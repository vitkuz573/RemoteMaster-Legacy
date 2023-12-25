// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Forms;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IFileManagerService
{
    Task UploadFileAsync(string path, string fileName, byte[] fileData);

    Stream DownloadFile(string path);

    Task<List<FileSystemItem>> GetFilesAndDirectoriesAsync(string path);

    Task<List<string>> GetAvailableDrivesAsync();
}
