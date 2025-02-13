// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
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
            var dbusDisplay = await GetDisplayFromDBus();

            if (!string.IsNullOrWhiteSpace(dbusDisplay))
            {
                return dbusDisplay;
            }
        }
        catch (Exception ex)
        {
            logger.LogError("DBus display lookup failed: {Message}", ex.Message);
        }

        return GetDisplayFallback();
    }

    private async Task<string> GetDisplayFromDBus()
    {
        using var connection = new Connection(Address.System);

        await connection.ConnectAsync();

        var loginManager = connection.CreateProxy<ILoginManager>("org.freedesktop.login1", "/org/freedesktop/login1");
        var pid = (uint)processService.GetCurrentProcess().Id;
        var sessionPath = await loginManager.GetSessionByPIDAsync(pid);
        var loginSession = connection.CreateProxy<ILoginSession>("org.freedesktop.login1", sessionPath);
        var display = await loginSession.GetDisplayAsync();

        return display;
    }

    private string GetDisplayFallback()
    {
        var xorgProcesses = processService.GetProcessesByName("Xorg");

        foreach (var process in xorgProcesses)
        {
            try
            {
                var args = commandLineProvider.GetCommandLine(process);
                
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

                    var linkPath = $"/proc/{process.Id}/fd/{fd}";
                    var fileInfo = fileSystem.FileInfo.New(linkPath);

                    if (!string.IsNullOrWhiteSpace(fileInfo.LinkTarget) && !fileInfo.LinkTarget.StartsWith("socket:", StringComparison.Ordinal) && !fileInfo.LinkTarget.StartsWith("pipe:", StringComparison.Ordinal))
                    {
                        return fileInfo.LinkTarget;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
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
    public string GetXAuthority()
    {
        var xorgProcesses = processService.GetProcessesByName("Xorg");

        foreach (var process in xorgProcesses)
        {
            try
            {
                var args = commandLineProvider.GetCommandLine(process);
                
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
