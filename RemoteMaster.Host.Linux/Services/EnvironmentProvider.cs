// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using RemoteMaster.Host.Linux.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class EnvironmentProvider : IEnvironmentProvider
{
    public string GetDisplay()
    {
        var xorgProcesses = Process.GetProcessesByName("Xorg");

        foreach (var proc in xorgProcesses)
        {
            try
            {
                var cmdLine = GetCommandLine(proc);
                var match = Regex.Match(cmdLine, @"\s(?<display>:\d+)\b");

                if (match.Success)
                {
                    var display = match.Groups["display"].Value;

                    if (!string.IsNullOrEmpty(display))
                    {
                        return display;
                    }
                }

                var args = SplitCommandLine(cmdLine);
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i] == "-displayfd" && i + 1 < args.Count)
                    {
                        if (int.TryParse(args[i + 1], out int fd))
                        {
                            string linkPath = $"/proc/{proc.Id}/fd/{fd}";
                            var fi = new FileInfo(linkPath);
                            if (!string.IsNullOrEmpty(fi.LinkTarget) && !fi.LinkTarget.StartsWith("socket:"))
                            {
                                return fi.LinkTarget;
                            }
                        }
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        for (int i = 0; i < 10; i++)
        {
            string socketPath = $"/tmp/.X11-unix/X{i}";
            if (File.Exists(socketPath))
            {
                return $":{i}";
            }
        }
        return ":0";
    }

    public string GetXAuthority()
    {
        var xorgProcesses = Process.GetProcessesByName("Xorg");
        foreach (var proc in xorgProcesses)
        {
            try
            {
                string cmdLine = GetCommandLine(proc);
                var match = Regex.Match(cmdLine, @"-auth\s+(\S+)");
                if (match.Success)
                {
                    string xAuth = match.Groups[1].Value;
                    if (!string.IsNullOrEmpty(xAuth))
                    {
                        return xAuth;
                    }
                }
                var args = SplitCommandLine(cmdLine);
                for (int i = 0; i < args.Count; i++)
                {
                    if (args[i] == "-auth" && i + 1 < args.Count)
                    {
                        string xAuth = args[i + 1];
                        if (!string.IsNullOrEmpty(xAuth))
                        {
                            return xAuth;
                        }
                    }
                }
            }
            catch
            {
                continue;
            }
        }
        return string.Empty;
    }

    private static string GetCommandLine(Process process)
    {
        string path = $"/proc/{process.Id}/cmdline";
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            return string.Join(" ", Encoding.UTF8.GetString(bytes).Split('\0', StringSplitOptions.RemoveEmptyEntries));
        }
        return string.Empty;
    }

    private static List<string> SplitCommandLine(string commandLine)
    {
        var args = new List<string>();
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return args;
        }

        bool inQuotes = false;
        var currentArg = new StringBuilder();
        foreach (char c in commandLine)
        {
            if (c == '\"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }
            }
            else
            {
                currentArg.Append(c);
            }
        }
        if (currentArg.Length > 0)
        {
            args.Add(currentArg.ToString());
        }
        return args;
    }
}