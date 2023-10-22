// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using RemoteMaster.Host.Windows.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class NetworkDriveService : INetworkDriveService
{
    private readonly ILogger<NetworkDriveService> _logger;

    public NetworkDriveService(ILogger<NetworkDriveService> logger)
    {
        _logger = logger;
    }

    public unsafe void MapNetworkDrive(string remotePath, string username, string password)
    {
        _logger.LogInformation("Attempting to map network drive with remote path: {RemotePath}", remotePath);

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
                    _logger.LogWarning("Network drive with remote path {RemotePath} is already assigned.", remotePath);
                    
                    return;
                }

                _logger.LogError("Failed to map network drive with remote path {RemotePath}. Error code: {Result}", remotePath, (int)result);
                
                throw new Win32Exception((int)result);
            }

            _logger.LogInformation("Successfully mapped network drive with remote path: {RemotePath}", remotePath);
        }
    }

    public unsafe void CancelNetworkDrive(string remotePath)
    {
        _logger.LogInformation("Attempting to cancel network drive with remote path: {RemotePath}", remotePath);

        fixed (char* pRemotePath = remotePath)
        {
            var result = WNetCancelConnection2W(pRemotePath, 0, true);

            if (result != WIN32_ERROR.NO_ERROR)
            {
                _logger.LogError("Failed to cancel network drive with remote path {RemotePath}. Error code: {Result}", remotePath, (int)result);
                
                throw new Win32Exception((int)result);
            }

            _logger.LogInformation("Successfully canceled network drive with remote path: {RemotePath}", remotePath);
        }
    }
}
