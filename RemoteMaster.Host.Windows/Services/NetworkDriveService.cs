// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class NetworkDriveService(ILogger<NetworkDriveService> logger) : INetworkDriveService
{
    public bool MapNetworkDrive(string remotePath, string? username, string? password)
    {
        logger.LogInformation("Attempting to map network drive with remote path: {RemotePath}", remotePath);

        var netResource = new NETRESOURCEW
        {
            dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK
        };

        unsafe
        {
            fixed (char* pRemotePath = remotePath)
            {
                netResource.lpRemoteName = pRemotePath;
            }
        }

        var result = WNetAddConnection2W(in netResource, password, username, 0);

        if (result != WIN32_ERROR.NO_ERROR)
        {
            if (result == WIN32_ERROR.ERROR_ALREADY_ASSIGNED)
            {
                logger.LogWarning("Network drive with remote path {RemotePath} is already assigned.", remotePath);

                return true;
            }

            logger.LogError("Failed to map network drive with remote path {RemotePath}. Error code: {ErrorValue} ({ErrorCode})", remotePath, result.ToString(), (int)result);

            return false;
        }

        logger.LogInformation("Successfully mapped network drive with remote path: {RemotePath}", remotePath);

        return true;
    }

    public bool CancelNetworkDrive(string remotePath)
    {
        logger.LogInformation("Attempting to cancel network drive with remote path: {RemotePath}", remotePath);

        WIN32_ERROR result;

        unsafe
        {
            fixed (char* pRemotePath = remotePath)
            {
                result = WNetCancelConnection2W(pRemotePath, 0, true);
            }
        }

        if (result != WIN32_ERROR.NO_ERROR)
        {
            logger.LogError("Failed to cancel network drive with remote path {RemotePath}. Error code: {ErrorValue} ({ErrorCode})", remotePath, result.ToString(), (int)result);

            return false;
        }

        logger.LogInformation("Successfully canceled network drive with remote path: {RemotePath}", remotePath);

        return true;
    }

    public string GetEffectivePath(string remotePath)
    {
        return remotePath;
    }
}
