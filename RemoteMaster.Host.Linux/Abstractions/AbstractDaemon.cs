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

    public async Task<bool> IsInstalledAsync()
    {
        var unitFilePath = $"/etc/systemd/system/{Name}.service";

        if (!fileSystem.File.Exists(unitFilePath))
        {
            logger.LogDebug("Unit file {UnitFilePath} does not exist.", unitFilePath);

            return false;
        }

        return await IsServiceEnabledAsync();
    }

    public async virtual Task<bool> IsRunningAsync()
    {
        if (!await IsInstalledAsync())
        {
            return false;
        }

        try
        {
            var process = processWrapperFactory.Create();

            var startInfo = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = $"is-active {Name}.service",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start(startInfo);

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (process.ExitCode == 0 && output.Trim().Equals("active", StringComparison.OrdinalIgnoreCase))
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

    public async virtual Task CreateAsync()
    {
        try
        {
            var unitFileContent = GenerateSystemdUnitFile();
            var unitFilePath = $"/etc/systemd/system/{Name}.service";

            fileSystem.File.WriteAllText(unitFilePath, unitFileContent);
            
            logger.LogInformation("Systemd unit file created at {UnitFilePath}.", unitFilePath);

            await ExecuteDaemonCommandAsync("daemon-reload");
            
            logger.LogInformation("Systemd daemon reloaded.");

            await ExecuteDaemonCommandAsync($"enable {Name}.service");

            logger.LogInformation("{Name} service enabled to start on boot.", Name);

            await ExecuteDaemonCommandAsync($"start {Name}.service");

            logger.LogInformation("{Name} service started successfully.", Name);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create {Name} daemon.", Name);
        }
    }

    public async virtual Task DeleteAsync()
    {
        try
        {
            await ExecuteDaemonCommandAsync($"stop {Name}.service");
            
            logger.LogInformation("{Name} service stopped.", Name);

            await ExecuteDaemonCommandAsync($"disable {Name}.service");
            
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

            await ExecuteDaemonCommandAsync("daemon-reload");
            
            logger.LogInformation("Systemd daemon reloaded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete {Name} daemon.", Name);
        }
    }

    public async virtual Task StartAsync()
    {
        if (await IsRunningAsync())
        {
            logger.LogWarning("{Name} daemon is already running.", Name);
            
            return;
        }

        logger.LogInformation("Starting {Name} daemon...", Name);
        
        await ExecuteDaemonCommandAsync($"start {Name}.service");
        
        logger.LogInformation("{Name} daemon started.", Name);
    }

    public async virtual Task StopAsync()
    {
        if (!await IsRunningAsync())
        {
            logger.LogWarning("{Name} daemon is not running.", Name);   
        }

        logger.LogInformation("Stopping {Name} daemon...", Name);
        
        await ExecuteDaemonCommandAsync($"stop {Name}.service");
        
        logger.LogInformation("{Name} daemon stopped.", Name);
    }

    public async virtual Task RestartAsync()
    {
        logger.LogInformation("Restarting {Name} daemon...", Name);
        
        await StopAsync();
        await StartAsync();
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

    protected async virtual Task ExecuteDaemonCommandAsync(string arguments)
    {
        var process = processWrapperFactory.Create();

        process.Start(new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command 'systemctl {arguments}' exited with code {process.ExitCode}: {error}");
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

    private async Task<bool> IsServiceEnabledAsync()
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

            var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
            var error = (await process.StandardError.ReadToEndAsync()).Trim();

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
