// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class InstanceStarterService : IInstanceStarterService
{
    public void StartNewInstance(string sourcePath, string executablePath, string arguments)
    {
        try
        {
            var executableDirectory = Path.GetDirectoryName(executablePath);

            if (!Directory.Exists(executableDirectory))
            {
                Log.Information("Creating directory {ExecutableDirectory} for the executable.", executableDirectory);
                Directory.CreateDirectory(executableDirectory);
            }

            Log.Information("Copying executable from {SourcePath} to {ExecutablePath}", sourcePath, executablePath);
            File.Copy(sourcePath, executablePath, true);
            Log.Information("Successfully copied the executable.");

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo(executablePath, arguments)
            {
                CreateNoWindow = true
            };

            process.Start();

            Log.Information("Started a new instance of the host with options: {@Options}", SafeLogArguments(process.StartInfo));
        }
        catch (IOException ioEx)
        {
            Log.Error(ioEx, "IO error occurred while copying the executable. Source: {SourcePath}, Destination: {ExecutablePath}", sourcePath, executablePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", executablePath);
        }
    }

    private static object SafeLogArguments(ProcessStartInfo startInfo)
    {
        var safeArguments = startInfo.Arguments.Replace(ExtractSensitivePart(startInfo.Arguments, "--username="), "[USERNAME]")
                                               .Replace(ExtractSensitivePart(startInfo.Arguments, "--password="), "[PASSWORD]");

        return new
        {
            startInfo.FileName,
            Arguments = safeArguments
        };
    }

    private static string ExtractSensitivePart(string arguments, string prefix)
    {
        var startIndex = arguments.IndexOf(prefix);

        if (startIndex == -1)
        {
            return string.Empty;
        }

        startIndex += prefix.Length;
        var endIndex = arguments.IndexOf(' ', startIndex);
        endIndex = endIndex == -1 ? arguments.Length : endIndex;

        return arguments[startIndex..endIndex];
    }
}