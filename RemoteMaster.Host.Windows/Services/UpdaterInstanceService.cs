// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Models;
using Serilog;
using System.Text;

namespace RemoteMaster.Host.Windows.Services;

public class UpdaterInstanceService : IUpdaterInstanceService
{
    private readonly string _argument = $"--launch-mode={LaunchMode.Updater.ToString().ToLower()}";
    private readonly string _sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "RemoteMaster.Host.exe");
    private readonly string _executablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater", "RemoteMaster.Host.exe");

    public void Start(string folderPath, string? username, string? password)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        var additionalArguments = BuildArguments(folderPath, username, password);

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

    private string BuildArguments(string folderPath, string? username, string? password)
    {
        var arguments = new StringBuilder(_argument);
        var escapedFolderPath = "\"" + folderPath.Replace("\"", "\\\"") + "\"";

        arguments.Append($" --folder-path={escapedFolderPath}");

        if (!string.IsNullOrEmpty(username))
        {
            var escapedUsername = "\"" + username.Replace("\"", "\\\"") + "\"";
            arguments.Append($" --username={escapedUsername}");
        }

        if (string.IsNullOrEmpty(password))
        {
            return arguments.ToString();
        }

        var escapedPassword = "\"" + password.Replace("\"", "\\\"") + "\"";
        arguments.Append($" --password={escapedPassword}");

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

            using var process = new NativeProcess();

            process.StartInfo = new NativeProcessStartInfo(_executablePath, additionalArguments)
            {
                ForceConsoleSession = true,
                DesktopName = "Default",
                CreateNoWindow = true,
                UseCurrentUserToken = false
            };

            process.Start();

            Log.Information("Started a new instance of the host with options: {@Options}", process.StartInfo);
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
}
