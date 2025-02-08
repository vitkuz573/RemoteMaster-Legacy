// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public partial class EnvironmentProvider(IProcessService processService, ICommandLineProvider commandLineProvider, IFileSystem fileSystem) : IEnvironmentProvider
{
    [GeneratedRegex(@"\s(?<display>:\d+)\b", RegexOptions.None, 1000)]
    private static partial Regex DisplayRegex();

    [GeneratedRegex(@"-auth\s+(\S+)", RegexOptions.None, 1000)]
    private static partial Regex XAuthRegex();

    public string GetDisplay()
    {
        var xorgProcesses = processService.GetProcessesByName("Xorg");

        foreach (var proc in xorgProcesses)
        {
            try
            {
                var args = commandLineProvider.GetCommandLine(proc);

                var cmdLine = string.Join(" ", proc);
                var match = DisplayRegex().Match(cmdLine);

                if (match.Success)
                {
                    var display = match.Groups["display"].Value;

                    if (!string.IsNullOrEmpty(display))
                    {
                        return display;
                    }
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != "-displayfd" || i + 1 >= args.Length)
                    {
                        continue;
                    }

                    if (!int.TryParse(args[i + 1], out var fd))
                    {
                        continue;
                    }

                    var linkPath = $"/proc/{proc.Id}/fd/{fd}";
                    var fi = fileSystem.FileInfo.New(linkPath);

                    if (!string.IsNullOrEmpty(fi.LinkTarget) && !fi.LinkTarget.StartsWith("socket:"))
                    {
                        return fi.LinkTarget;
                    }
                }
            }
            catch
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

    public string GetXAuthority()
    {
        var xorgProcesses = processService.GetProcessesByName("Xorg");
        
        foreach (var proc in xorgProcesses)
        {
            try
            {
                var args = commandLineProvider.GetCommandLine(proc);

                var cmdLine = string.Join(" ", args);
                var match = XAuthRegex().Match(cmdLine);

                if (match.Success)
                {
                    var xAuth = match.Groups[1].Value;

                    if (!string.IsNullOrEmpty(xAuth))
                    {
                        return xAuth;
                    }
                }

                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] != "-auth" || i + 1 >= args.Length)
                    {
                        continue;
                    }

                    var xAuth = args[i + 1];

                    if (!string.IsNullOrEmpty(xAuth))
                    {
                        return xAuth;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        return string.Empty;
    }
}
