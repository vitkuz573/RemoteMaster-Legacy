// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Models;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// A hosted service that checks for required dependencies on startup
/// and installs any that are missing.
/// </summary>
public class DependencyInstallerService(IProcessWrapperFactory processWrapperFactory, ILogger<DependencyInstallerService> logger) : IHostedService
{
    private readonly Dependency[] _dependencies =
    [
        new()
        {
            Name = "libX11",
            CheckCommand = "dpkg -l | grep libx11-dev",
            InstallCommand = "sudo apt-get install -y libx11-dev"
        },
        new()
        {
            Name = "libXrandr",
            CheckCommand = "dpkg -l | grep libxrandr-dev",
            InstallCommand = "sudo apt-get install -y libxrandr-dev"
        },
        new()
        {
            Name = "libXtst",
            CheckCommand = "dpkg -l | grep libxtst-dev",
            InstallCommand = "sudo apt-get install -y libxtst-dev"
        }
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Dependency Installer Service is starting.");

        foreach (var dependency in _dependencies)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancellation requested, stopping dependency check.");
                break;
            }

            logger.LogInformation("Checking dependency: {DependencyName}", dependency.Name);
            
            var isInstalled = await IsDependencyInstalledAsync(dependency, cancellationToken);

            if (!isInstalled)
            {
                logger.LogWarning("Dependency {DependencyName} is not installed. Attempting installation...", dependency.Name);
                
                var installedSuccessfully = await InstallDependencyAsync(dependency, cancellationToken);

                if (installedSuccessfully)
                {
                    logger.LogInformation("Dependency {DependencyName} was installed successfully.", dependency.Name);
                }
                else
                {
                    logger.LogError("Failed to install dependency: {DependencyName}", dependency.Name);
                }
            }
            else
            {
                logger.LogInformation("Dependency {DependencyName} is already installed.", dependency.Name);
            }
        }

        logger.LogInformation("Dependency Installer Service has finished its work.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Dependency Installer Service is stopping.");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a dependency is installed by executing its check command.
    /// </summary>
    private async Task<bool> IsDependencyInstalledAsync(Dependency dependency, CancellationToken cancellationToken)
    {
        try
        {
            var process = processWrapperFactory.Create();
            
            process.Start(new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{dependency.CheckCommand}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                logger.LogDebug("Check output for {DependencyName}: {Output}", dependency.Name, output);
                
                return true;
            }
            else
            {
                logger.LogDebug("Check command for {DependencyName} exited with code {ExitCode}. Error: {Error}", dependency.Name, process.ExitCode, error);
                
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking dependency {DependencyName}", dependency.Name);
           
            return false;
        }
    }

    /// <summary>
    /// Installs a dependency by executing its installation command.
    /// </summary>
    private async Task<bool> InstallDependencyAsync(Dependency dependency, CancellationToken cancellationToken)
    {
        try
        {
            var process = processWrapperFactory.Create();
            
            process.Start(new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{dependency.InstallCommand}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                logger.LogDebug("Install output for {DependencyName}: {Output}", dependency.Name, output);
                
                return true;
            }
            else
            {
                logger.LogError("Install command for {DependencyName} exited with code {ExitCode}. Error: {Error}", dependency.Name, process.ExitCode, error);
                
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while installing dependency {DependencyName}", dependency.Name);
            
            return false;
        }
    }
}
