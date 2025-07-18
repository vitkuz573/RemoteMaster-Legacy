﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Extensions;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// Provides environment details for Linux systems.
/// </summary>
public partial class EnvironmentProvider(IProcessService processService, ICommandLineProvider commandLineProvider, IFileSystem fileSystem, ILogger<EnvironmentProvider> logger) : IEnvironmentProvider
{
    [GeneratedRegex(@"\s(?<display>:\d+)\b", RegexOptions.None, 1000)]
    private static partial Regex DisplayRegex();

    [GeneratedRegex(@"-auth\s+(\S+)", RegexOptions.None, 1000)]
    private static partial Regex XAuthRegex();

    /// <inheritdoc/>
    public async Task<string> GetDisplayAsync()
    {
        try
        {
            var dbusDisplay = await GetDisplayFromDBusAsync();

            if (!string.IsNullOrWhiteSpace(dbusDisplay))
            {
                return dbusDisplay;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("DBus display lookup failed: {Message}", ex.Message);
        }

        return await GetDisplayFallbackAsync();
    }

    private async Task<string> GetDisplayFromDBusAsync()
    {
        using var connection = new Connection(Address.System);
        await connection.ConnectAsync();

        var loginManager = connection.CreateProxy<ILoginManager>("org.freedesktop.login1", "/org/freedesktop/login1");

        var xorgProcesses = processService.GetProcessesByName("Xorg");
        
        foreach (var process in xorgProcesses)
        {
            try
            {
                var pid = (uint)process.Id;
                var sessionPath = await loginManager.GetSessionByPIDAsync(pid);
                var loginSession = connection.CreateProxy<ILoginSession>("org.freedesktop.login1", sessionPath);
                var display = await loginSession.GetDisplayAsync();

                if (!string.IsNullOrWhiteSpace(display))
                {
                    return display;
                }
            }
            catch (Exception ex)
            {
                logger.LogError("DBus display lookup failed for Xorg process with PID {Pid}: {Message}", process.Id, ex.Message);
            }
        }

        return string.Empty;
    }

    private async Task<string> GetDisplayFallbackAsync()
    {
        var xorgProcesses = processService.GetProcessesByName("Xorg");

        foreach (var process in xorgProcesses)
        {
            try
            {
                var args = await commandLineProvider.GetCommandLineAsync(process);
   
                var commandLine = string.Join(" ", args);
                var displayMatch = DisplayRegex().Match(commandLine);

                if (displayMatch.Success)
                {
                    var display = displayMatch.Groups["display"].Value;

                    if (!string.IsNullOrWhiteSpace(display))
                    {
                        return display;
                    }
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != "-displayfd" || i + 1 >= args.Length || !int.TryParse(args[i + 1], out var fd))
                    {
                        continue;
                    }

                    var fdPath = $"/proc/{process.Id}/fd/{fd}";

                    try
                    {
                        using var stream = fileSystem.File.OpenRead(fdPath);
                        using var reader = new StreamReader(stream);
                        var displayData = (await reader.ReadToEndAsync())?.Trim();

                        if (!string.IsNullOrWhiteSpace(displayData))
                        {
                            return displayData;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error reading displayfd for process {Pid}: {Message}", process.Id, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error processing Xorg process with PID {Pid}: {Message}", process.Id, ex.Message);
            }
        }

        for (var i = 0; i < 10; i++)
        {
            var socketPath = $"/tmp/.X11-unix/X{i}";

            if (fileSystem.File.Exists(socketPath))
            {
                return $":{i}";
            }
        }

        return ":0";
    }

    /// <inheritdoc/>
    public async Task<string> GetXAuthorityAsync()
    {
        var xorgProcesses = processService.GetProcessesByName("Xorg");

        foreach (var process in xorgProcesses)
        {
            try
            {
                var args = await commandLineProvider.GetCommandLineAsync(process);
                
                var commandLine = string.Join(" ", args);
                var authMatch = XAuthRegex().Match(commandLine);

                if (authMatch.Success)
                {
                    var xAuthority = authMatch.Groups[1].Value;
                    
                    if (!string.IsNullOrWhiteSpace(xAuthority))
                    {
                        return xAuthority;
                    }
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != "-auth" || i + 1 >= args.Length)
                    {
                        continue;
                    }

                    var xAuthority = args[i + 1];

                    if (!string.IsNullOrWhiteSpace(xAuthority))
                    {
                        return xAuthority;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return string.Empty;
    }
}
