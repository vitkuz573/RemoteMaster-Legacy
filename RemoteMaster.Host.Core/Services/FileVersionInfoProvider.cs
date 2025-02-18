// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class FileVersionInfoProvider(IFileSystem fileSystem) : IFileVersionInfoProvider
{
    public string GetFileVersion(string executablePath)
    {
        var versionInfo = fileSystem.FileVersionInfo.GetVersionInfo(executablePath);

        return versionInfo.FileVersion ?? string.Empty;
    }

    public bool FileExists(string path)
    {
        return fileSystem.File.Exists(path);
    }
}
