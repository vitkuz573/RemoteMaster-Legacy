// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ApplicationPathProvider(IFileSystem fileSystem) : IApplicationPathProvider
{
    private readonly string _programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    private readonly string _commonAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

    public string RootDirectory => fileSystem.Path.Combine(_programFilesPath, "RemoteMaster", "Host");

    public string UpdateDirectory => fileSystem.Path.Combine(RootDirectory, "Update");

    public string UpdaterDirectory => fileSystem.Path.Combine(RootDirectory, "Updater");

    public string DataDirectory => fileSystem.Path.Combine(_commonAppDataPath, "RemoteMaster", "Host");
}
