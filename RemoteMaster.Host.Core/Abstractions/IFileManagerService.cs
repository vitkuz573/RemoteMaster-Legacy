// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components.Forms;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IFileManagerService
{
    Task<FileInfo[]> GetFilesAsync(string path);

    Task<DirectoryInfo[]> GetDirectoriesAsync(string path);

    Task UploadFileAsync(string path, IBrowserFile file);

    Stream DownloadFile(string path);
}
