// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Helpers;

public abstract class NetworkDriveHelper
{
    public static unsafe void MapNetworkDrive(string remotePath, string username, string password)
    {
        var netResource = new NETRESOURCEW
        {
            dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK
        };

        fixed (char* pRemotePath = remotePath)
        {
            netResource.lpRemoteName = pRemotePath;

            var result = WNetAddConnection2W(in netResource, password, username, 0);

            if (result != WIN32_ERROR.NO_ERROR)
            {
                if (result == WIN32_ERROR.ERROR_ALREADY_ASSIGNED)
                {
                    return;
                }

                throw new Win32Exception((int)result);
            }
        }
    }

    public static unsafe void CancelNetworkDrive(string remotePath)
    {
        fixed (char* pRemotePath = remotePath)
        {
            var result = WNetCancelConnection2W(pRemotePath, 0, true);

            if (result != WIN32_ERROR.NO_ERROR)
            {
                throw new Win32Exception((int)result);
            }
        }
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true, bool overwriteExisting = false)
    {
        var sourceDir = new DirectoryInfo(sourceDirName);

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
