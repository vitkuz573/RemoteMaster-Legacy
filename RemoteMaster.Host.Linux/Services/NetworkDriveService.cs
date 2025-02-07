// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class NetworkDriveService : INetworkDriveService
{
    private readonly ILogger<NetworkDriveService> _logger;

    private const string BaseMountPoint = "/mnt/RemoteMaster";

    public NetworkDriveService(ILogger<NetworkDriveService> logger)
    {
        _logger = logger;

        if (!Directory.Exists(BaseMountPoint))
        {
            Directory.CreateDirectory(BaseMountPoint);
        }
    }

    public bool MapNetworkDrive(string remotePath, string? username, string? password)
    {
        ArgumentNullException.ThrowIfNull(remotePath);

        remotePath = remotePath.TrimEnd('\\', '/');

        _logger.LogInformation("Attempting to map network drive with remote path: {RemotePath}", remotePath);

        try
        {
            var mountPoint = GetMountPoint(remotePath);

            if (!Directory.Exists(mountPoint))
            {
                Directory.CreateDirectory(mountPoint);
            }

            string options;

            if (!string.IsNullOrWhiteSpace(username))
            {
                options = $"username={username}";

                if (!string.IsNullOrWhiteSpace(password))
                {
                    options += $",password={password}";
                }
            }
            else
            {
                options = "guest";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "mount",
                Arguments = $"-t cifs \"{remotePath}\" \"{mountPoint}\" -o {options}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();

                    _logger.LogError("Failed to map network drive with remote path {RemotePath}. Error: {Error}", remotePath, error);

                    return false;
                }
            }

            _logger.LogInformation("Successfully mapped network drive with remote path: {RemotePath} to mount point: {MountPoint}", remotePath, mountPoint);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while mapping network drive with remote path: {RemotePath}", remotePath);

            return false;
        }
    }

    public bool CancelNetworkDrive(string remotePath)
    {
        ArgumentNullException.ThrowIfNull(remotePath);
        
        _logger.LogInformation("Attempting to cancel network drive with remote path: {RemotePath}", remotePath);

        try
        {
            var mountPoint = GetMountPoint(remotePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "umount",
                Arguments = $"\"{mountPoint}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                        
                    _logger.LogError("Failed to cancel network drive with remote path {RemotePath}. Error: {Error}", remotePath, error);
                        
                    return false;
                }
            }

            if (Directory.Exists(mountPoint))
            {
                Directory.Delete(mountPoint);
            }

            _logger.LogInformation("Successfully canceled network drive with remote path: {RemotePath}", remotePath);
                
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while canceling network drive with remote path: {RemotePath}", remotePath);

            return false;
        }
    }

    public string GetEffectivePath(string remotePath)
    {
        ArgumentNullException.ThrowIfNull(remotePath);

        return GetMountPoint(remotePath);
    }

    private static string GetMountPoint(string remotePath)
    {
        var sanitized = remotePath.TrimStart('/').Replace('/', '_');

        return Path.Combine(BaseMountPoint, sanitized);
    }
}
