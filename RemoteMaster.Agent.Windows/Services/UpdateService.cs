// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Agent.Services;

public class UpdateService : IUpdateService
{
    private const string SharedFolder = @"\\SERVER-DC02\Win\RemoteMaster";
    private const string Login = "support@it-ktk.local";
    private const string Password = "teacher123!!";

    private readonly ILogger<UpdateService> _logger;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
    }

    public void InstallClient()
    {
        try
        {
            _logger.LogInformation("Installing client...");
            MapNetworkDrive(SharedFolder, Login, Password);
            DirectoryCopy(SharedFolder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client");
            CancelNetworkDrive(SharedFolder);
            _logger.LogInformation("Client installed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while installing client: {Message}", ex.Message);
            throw;
        }
    }

    public void UpdateClient()
    {
        try
        {
            var processes = Process.GetProcessesByName("RemoteMaster.Client");

            if (processes.Length == 0)
            {
                _logger.LogInformation("No RemoteMaster.Client processes found.");
            }
            else
            {
                _logger.LogInformation("Found {ProcessCount} RemoteMaster.Client processes.", processes.Length);

                foreach (var client in processes)
                {
                    _logger.LogInformation("Attempting to kill process with ID: {ClientID}", client.Id);
                    client.Kill();
                    _logger.LogInformation("Process with ID: {ClientID} has been killed.", client.Id);
                }
            }

            Thread.Sleep(10000);

            _logger.LogInformation("Updating client...");
            MapNetworkDrive(SharedFolder, Login, Password);
            DirectoryCopy(SharedFolder, $"{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)}/RemoteMaster/Client", true, true);
            CancelNetworkDrive(SharedFolder);
            _logger.LogInformation("Client updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating client: {Message}", ex.Message);
            throw;
        }
    }

    private unsafe void MapNetworkDrive(string remotePath, string username, string password)
    {
        try
        {
            _logger.LogDebug("Mapping network drive: {RemotePath} with user: {Username}.", remotePath, username);

            var netResource = new NETRESOURCEW
            {
                dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK
            };

            fixed (char* pRemotePath = remotePath)
            {
                netResource.lpRemoteName = pRemotePath;

                var result = WNetAddConnection2W(in netResource, password, username, 0);

                if (result != (uint)WIN32_ERROR.NO_ERROR)
                {
                    if (result == (uint)WIN32_ERROR.ERROR_ALREADY_ASSIGNED)
                    {
                        return;
                    }

                    throw new Win32Exception((int)result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map network drive: {RemotePath}. Message: {Message}", remotePath, ex.Message);
            throw;
        }
    }

    private unsafe void CancelNetworkDrive(string remotePath)
    {
        try
        {
            _logger.LogDebug("Disconnecting network drive: {RemotePath}.", remotePath);

            fixed (char* pRemotePath = remotePath)
            {
                var result = WNetCancelConnection2W(pRemotePath, 0, true);

                if (result != (uint)WIN32_ERROR.NO_ERROR)
                {
                    throw new Win32Exception((int)result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect network drive: {RemotePath}. Message: {Message}", remotePath, ex.Message);
            throw;
        }
    }

    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs = true, bool overwriteExisting = false)
    {
        try
        {
            _logger.LogDebug("Copying directory from {SourceDir} to {DestDir}.", sourceDirName, destDirName);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy directory from {SourceDir} to {DestDir}. Message: {Message}", sourceDirName, destDirName, ex.Message);
            throw;
        }
    }
}
