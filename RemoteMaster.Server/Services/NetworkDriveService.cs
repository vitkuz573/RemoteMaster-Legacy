// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using FluentResults;
using RemoteMaster.Server.Abstractions;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class NetworkDriveService : INetworkDriveService
{
    public Result MapNetworkDrive(string remotePath, string? username, string? password)
    {
        Log.Information("Attempting to map network drive with remote path: {RemotePath}", remotePath);

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
                Log.Warning("Network drive with remote path {RemotePath} is already assigned.", remotePath);
                
                return Result.Ok();
            }

            Log.Error("Failed to map network drive with remote path {RemotePath}. Error code: {ErrorValue} ({ErrorCode})", remotePath, result.ToString(), (int)result);
            
            return Result.Fail($"Failed to map network drive with remote path {remotePath}. Error code: {result} ({(int)result})");
        }

        Log.Information("Successfully mapped network drive with remote path: {RemotePath}", remotePath);
        
        return Result.Ok();
    }

    public Result CancelNetworkDrive(string remotePath)
    {
        Log.Information("Attempting to cancel network drive with remote path: {RemotePath}", remotePath);

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
            Log.Error("Failed to cancel network drive with remote path {RemotePath}. Error code: {ErrorValue} ({ErrorCode})", remotePath, result.ToString(), (int)result);
            
            return Result.Fail($"Failed to cancel network drive with remote path {remotePath}. Error code: {result} ({(int)result})");
        }

        Log.Information("Successfully canceled network drive with remote path: {RemotePath}", remotePath);
        
        return Result.Ok();
    }
}
