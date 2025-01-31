// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Abstractions;

public abstract class AbstractDaemon(IFileSystem fileSystem, ILogger<AbstractDaemon> logger) : IService
{
    public abstract string Name { get; }

    protected abstract string BinPath { get; }

    protected abstract IDictionary<string, string?> Arguments { get; }

    protected abstract string? Description { get; }

    public virtual bool IsInstalled
    {
        get
        {
            var unitFilePath = $"/etc/systemd/system/{Name}.service";

            if (fileSystem.File.Exists(unitFilePath))
            {
                return IsServiceEnabled();
            }

            logger.LogDebug($"Unit file {unitFilePath} does not exist.");

            return false;
        }
    }

    public virtual bool IsRunning
    {
        get
        {
            if (!IsInstalled)
            {
                return false;
            }

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = $"is-active {Name}.service",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process();
                process.StartInfo = processInfo;
                process.Start();

                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd().Trim();

                process.WaitForExit();

                if (process.ExitCode == 0 && output.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug($"{Name} service is active.");

                    return true;
                }

                logger.LogDebug($"{Name} service is not active: {output} {error}");

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to check if {Name} service is running.");

                return false;
            }
        }
    }

    public virtual void Create()
    {
        try
        {
            var unitFileContent = GenerateSystemdUnitFile();
            var unitFilePath = $"/etc/systemd/system/{Name}.service";

            fileSystem.File.WriteAllText(unitFilePath, unitFileContent);

            logger.LogInformation($"Systemd unit file created at {unitFilePath}.");

            ExecuteCommand("systemctl", "daemon-reload");

            logger.LogInformation("Systemd daemon reloaded.");

            ExecuteCommand("systemctl", $"enable {Name}.service");

            logger.LogInformation($"{Name} service enabled to start on boot.");

            ExecuteCommand("systemctl", $"start {Name}.service");

            logger.LogInformation($"{Name} service started successfully.");
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Failed to create {Name} daemon.");
        }
    }

    public virtual void Delete()
    {
        try
        {
            ExecuteCommand("systemctl", $"stop {Name}.service");

            logger.LogInformation($"{Name} service stopped.");

            ExecuteCommand("systemctl", $"disable {Name}.service");

            logger.LogInformation($"{Name} service disabled from starting on boot.");

            var unitFilePath = $"/etc/systemd/system/{Name}.service";

            if (fileSystem.File.Exists(unitFilePath))
            {
                fileSystem.File.Delete(unitFilePath);

                logger.LogInformation($"Systemd unit file deleted at {unitFilePath}.");
            }
            else
            {
                logger.LogWarning($"Systemd unit file not found at {unitFilePath}.");
            }

            ExecuteCommand("systemctl", "daemon-reload");

            logger.LogInformation("Systemd daemon reloaded.");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to delete {Name} daemon.");
        }
    }

    public virtual void Start()
    {
        if (IsRunning)
        {
            logger.LogWarning($"{Name} daemon is already running.");

            return;
        }

        logger.LogInformation($"Starting {Name} daemon...");
    }

    public virtual void Stop()
    {
        if (!IsRunning)
        {
            logger.LogWarning($"{Name} daemon is not running.");

            return;
        }

        logger.LogInformation($"Stopping {Name} daemon...");
    }

    public virtual void Restart()
    {
        logger.LogInformation($"Restarting {Name} daemon...");

        Stop();
        Start();
    }

    private string GenerateSystemdUnitFile()
    {
        return $"""
                [Unit]
                Description={Description}
                After=network.target

                [Service]
                Type=simple
                ExecStart={BinPath} {string.Join(" ", Arguments.Select(kv => kv.Value == null ? $"{kv.Key}" : $"{kv.Key}={kv.Value}"))}
                Restart=on-failure
                User=root
                Group=root

                [Install]
                WantedBy=multi-user.target
                
                """;
    }

    private void ExecuteCommand(string command, string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processInfo;
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command '{command} {arguments}' exited with code {process.ExitCode}: {error}");
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            logger.LogInformation(output);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogWarning(error);
        }
    }

    private bool IsServiceEnabled()
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = $"is-enabled {Name}.service",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            process.Start();

            var output = process.StandardOutput.ReadToEnd().Trim();
            var error = process.StandardError.ReadToEnd().Trim();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                logger.LogDebug($"{Name} service is enabled.");

                return output.Equals("enabled", StringComparison.OrdinalIgnoreCase);
            }

            logger.LogDebug($"{Name} service is not enabled: {error}");

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to check if {Name} service is enabled.");

            return false;
        }
    }
}
