// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.


using System.ComponentModel;
using System.IO;
using RemoteMaster.Agent.Core.Abstractions;
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Agent.Windows.Services;

public class UpdateService : IUpdateService
{
    private const string SharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
    private const string Login = "support@it-ktk.local";
    private const string Password = "teacher123!!";

    public void InstallClient()
    {
        MapNetworkDrive(SharedFolder, Login, Password);
        DirectoryCopy(SharedFolder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client");
    }

    public void UpdateClient()
    {
        MapNetworkDrive(SharedFolder, Login, Password);
        DirectoryCopy(SharedFolder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client", true, true);
    }

    private static unsafe void MapNetworkDrive(string remotePath, string username, string password)
    {
        var netResource = new NETRESOURCEW
        {
            dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK
        };

        fixed (char* pRemotePath = remotePath)
        {
            netResource.lpRemoteName = pRemotePath;

            var result = WNetAddConnection2W(in netResource, password, username, 0);

            if (result != 0)
            {
                throw new Win32Exception((int)result);
            }
        }
    }

    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true, bool overwriteExisting = false)
    {
        var sourceDir = new DirectoryInfo(sourceDirName);

        if (!sourceDir.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");
        }

        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        foreach (var file in sourceDir.GetFiles())
        {
            var destPath = Path.Combine(destDirName, file.Name);
            if (!File.Exists(destPath) || overwriteExisting)
            {
                file.CopyTo(destPath, true);
            }
        }

        if (copySubDirs)
        {
            foreach (var subdir in sourceDir.GetDirectories())
            {
                var destSubDir = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, destSubDir, true, overwriteExisting);
            }
        }
    }
}
