// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Text;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterInstanceService : IUpdaterInstanceService
{
    private readonly string _argument = $"--launch-mode=updater";
    private readonly string _sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");
    private readonly string _executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");

    public void Start(UpdateRequest updateRequest)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var additionalArguments = BuildArguments(updateRequest.FolderPath, updateRequest.UserCredentials, updateRequest.ForceUpdate, updateRequest.AllowDowngrade);

        try
        {
            StartNewInstance(additionalArguments);
            Log.Information("Successfully started a new instance of the host.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {ExecutablePath}", _executablePath);
        }
    }

    private string BuildArguments(string folderPath, Credentials? userCredentials, bool force, bool allowDowngrade)
    {
        var arguments = new StringBuilder(_argument);
        var escapedFolderPath = "\"" + folderPath.Replace("\"", "\\\"") + "\"";

        arguments.Append($" --folder-path={escapedFolderPath}");

        if (userCredentials == null)
        {
            return arguments.ToString();
        }

        if (!string.IsNullOrEmpty(userCredentials.Username))
        {
            var escapedUsername = "\"" + userCredentials.Username.Replace("\"", "\\\"") + "\"";
            arguments.Append($" --username={escapedUsername}");
        }

        if (string.IsNullOrEmpty(userCredentials.Password))
        {
            return arguments.ToString();
        }

        var escapedPassword = "\"" + userCredentials.Password.Replace("\"", "\\\"") + "\"";
        arguments.Append($" --password={escapedPassword}");

        if (force)
        {
            arguments.Append(" --force=true");
        }

        if (allowDowngrade)
        {
            arguments.Append(" --allow-downgrade=true");
        }

        return arguments.ToString();
    }

    private void StartNewInstance(string additionalArguments)
    {
        try
        {
            var executableDirectory = Path.GetDirectoryName(_executablePath);

            if (!Directory.Exists(executableDirectory))
            {
                Log.Information("Creating directory {ExecutableDirectory} for the executable.", executableDirectory);
                Directory.CreateDirectory(executableDirectory);
            }

            Log.Information("Copying executable from {SourcePath} to {ExecutablePath}", _sourcePath, _executablePath);
            File.Copy(_sourcePath, _executablePath, true);
            Log.Information("Successfully copied the executable.");

            using var process = new Process();

            process.StartInfo = new ProcessStartInfo(_executablePath, additionalArguments)
            {
                CreateNoWindow = true
            };

            process.Start();

            Log.Information("Started a new instance of the host with options: {@Options}", SafeLogArguments(process.StartInfo));
        }
        catch (IOException ioEx)
        {
            Log.Error(ioEx, "IO error occurred while copying the executable. Source: {SourcePath}, Destination: {ExecutablePath}", _sourcePath, _executablePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", _executablePath);
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
