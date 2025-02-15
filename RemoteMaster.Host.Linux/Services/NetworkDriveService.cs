// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class NetworkDriveService : INetworkDriveService
{
    private readonly IProcessWrapperFactory _processWrapperFactory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<NetworkDriveService> _logger;

    private const string BaseMountPoint = "/mnt/RemoteMaster";

    public NetworkDriveService(IProcessWrapperFactory processWrapperFactory, IFileSystem fileSystem, ILogger<NetworkDriveService> logger)
    {
        _processWrapperFactory = processWrapperFactory;
        _fileSystem = fileSystem;
        _logger = logger;

        if (!_fileSystem.Directory.Exists(BaseMountPoint))
        {
            _fileSystem.Directory.CreateDirectory(BaseMountPoint);
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

            if (!_fileSystem.Directory.Exists(mountPoint))
            {
                _fileSystem.Directory.CreateDirectory(mountPoint);
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

            var process = _processWrapperFactory.Create();

            process.Start(new ProcessStartInfo
            {
                FileName = "mount",
                Arguments = $"-t cifs \"{remotePath}\" \"{mountPoint}\" -o {options}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();

                _logger.LogError("Failed to map network drive with remote path {RemotePath}. Error: {Error}", remotePath, error);

                return false;
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

            var process = _processWrapperFactory.Create();

            process.Start(new ProcessStartInfo
            {
                FileName = "umount",
                Arguments = $"\"{mountPoint}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();

                _logger.LogError("Failed to cancel network drive with remote path {RemotePath}. Error: {Error}", remotePath, error);

                return false;
            }

            if (_fileSystem.Directory.Exists(mountPoint))
            {
                _fileSystem.Directory.Delete(mountPoint);
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

    private string GetMountPoint(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            throw new ArgumentException("Remote path cannot be null or empty.", nameof(remotePath));
        }

        var normalizedPath = remotePath.Replace('\\', '/');
        normalizedPath = normalizedPath.Trim('/');

        var intermediate = normalizedPath.Replace("/", "_");
        var invalidChars = _fileSystem.Path.GetInvalidFileNameChars();

        var sanitized = new string(intermediate.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());

        const int maxLength = 100;

        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return _fileSystem.Path.Combine(BaseMountPoint, sanitized);
    }
}
