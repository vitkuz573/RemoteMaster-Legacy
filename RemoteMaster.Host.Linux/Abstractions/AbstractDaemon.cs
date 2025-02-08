// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Abstractions;

public abstract class AbstractDaemon(IFileSystem fileSystem, IProcessWrapperFactory processWrapperFactory, ILogger<AbstractDaemon> logger) : IService
{
    public abstract string Name { get; }

    protected abstract string BinPath { get; }

    protected abstract string WorkingDirectory { get; }

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

            logger.LogDebug("Unit file {UnitFilePath} does not exist.", unitFilePath);

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
                var process = processWrapperFactory.Create();

                process.Start(new ProcessStartInfo
                {
                    FileName = "systemctl",
                    Arguments = $"is-active {Name}.service",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                var output = process.StandardOutput.ReadToEnd().Trim();
                var error = process.StandardError.ReadToEnd().Trim();

                process.WaitForExit();

                if (process.ExitCode == 0 && output.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug("{Name} service is active.", Name);

                    return true;
                }

                logger.LogDebug("{Name} service is not active: {Output} {Error}", Name, output, error);

                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check if {Name} service is running.", Name);

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

            logger.LogInformation("Systemd unit file created at {UnitFilePath}.", unitFilePath);

            ExecuteCommand("systemctl", "daemon-reload");

            logger.LogInformation("Systemd daemon reloaded.");

            ExecuteCommand("systemctl", $"enable {Name}.service");

            logger.LogInformation("{Name} service enabled to start on boot.", Name);

            ExecuteCommand("systemctl", $"start {Name}.service");

            logger.LogInformation("{Name} service started successfully.", Name);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create {Name} daemon.", Name);
        }
    }

    public virtual void Delete()
    {
        try
        {
            ExecuteCommand("systemctl", $"stop {Name}.service");

            logger.LogInformation("{Name} service stopped.", Name);

            ExecuteCommand("systemctl", $"disable {Name}.service");

            logger.LogInformation("{Name} service disabled from starting on boot.", Name);

            var unitFilePath = $"/etc/systemd/system/{Name}.service";

            if (fileSystem.File.Exists(unitFilePath))
            {
                fileSystem.File.Delete(unitFilePath);

                logger.LogInformation("Systemd unit file deleted at {UnitFilePath}.", unitFilePath);
            }
            else
            {
                logger.LogWarning("Systemd unit file not found at {UnitFilePath}.", unitFilePath);
            }

            ExecuteCommand("systemctl", "daemon-reload");

            logger.LogInformation("Systemd daemon reloaded.");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete {Name} daemon.", Name);
        }
    }

    public virtual void Start()
    {
        if (IsRunning)
        {
            logger.LogWarning("{Name} daemon is already running.", Name);

            return;
        }

        logger.LogInformation("Starting {Name} daemon...", Name);
    }

    public virtual void Stop()
    {
        if (!IsRunning)
        {
            logger.LogWarning("{Name} daemon is not running.", Name);

            return;
        }

        logger.LogInformation("Stopping {Name} daemon...", Name);
    }

    public virtual void Restart()
    {
        logger.LogInformation("Restarting {Name} daemon...", Name);

        Stop();
        Start();
    }

    private string GenerateSystemdUnitFile()
    {
        return $"""
                [Unit]
                Description={Description}

                [Service]
                WorkingDirectory={WorkingDirectory}
                ExecStartPre=/bin/sh -c 'while [ $(ls -1 /tmp/.X11-unix/X* 2>/dev/null | wc -l) -eq 0 ]; do echo "Waiting for X server..."; sleep 1; done'
                ExecStart={BinPath} {string.Join(" ", Arguments.Select(kv => kv.Value == null ? $"{kv.Key}" : $"{kv.Key}={kv.Value}"))}
                Restart=always
                StartLimitIntervalSec=0
                RestartSec=10

                [Install]
                WantedBy=graphical.target
                
                """;
    }

    private void ExecuteCommand(string command, string arguments)
    {
        var process = processWrapperFactory.Create();

        process.Start(new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command '{command} {arguments}' exited with code {process.ExitCode}: {error}");
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            logger.LogInformation("{Output}", output);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            logger.LogWarning("{Error}", error);
        }
    }

    private bool IsServiceEnabled()
    {
        try
        {
            var process = processWrapperFactory.Create();

            process.Start(new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = $"is-enabled {Name}.service",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            var output = process.StandardOutput.ReadToEnd().Trim();
            var error = process.StandardError.ReadToEnd().Trim();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                logger.LogDebug("{Name} service is enabled.", Name);

                return output.Equals("enabled", StringComparison.OrdinalIgnoreCase);
            }

            logger.LogDebug("{Name} service is not enabled: {Error}", Name, error);

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check if {Name} service is enabled.", Name);

            return false;
        }
    }
}
