// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Extensions;
using RemoteMaster.Host.Windows.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class UserInstanceService : IUserInstanceService
{
    private readonly LaunchMode _launchMode = LaunchMode.User;
    private readonly string _currentExecutablePath = Environment.ProcessPath!;

    public bool IsRunning => FindHostProcesses().Any(IsUserInstance);

    public void Start()
    {
        try
        {
            StartNewInstance();
            Log.Information("Successfully started a new instance of the host.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", _currentExecutablePath);
        }
    }

    public void Stop()
    {
        foreach (var process in FindHostProcesses())
        {
            if (!IsUserInstance(process))
            {
                continue;
            }

            try
            {
                process.Kill();
                Log.Information("Successfully stopped an instance of the host. Process ID: {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error stopping instances of the host. Message: {Message}", ex.Message);
            }
        }
    }

    private void StartNewInstance()
    {
        var launchArgument = $"--launch-mode={_launchMode.ToString().ToLower()}";
        using var process = new NativeProcess();

        process.StartInfo = new NativeProcessStartInfo(_currentExecutablePath, launchArgument)
        {
            ForceConsoleSession = true,
            DesktopName = "Default",
            CreateNoWindow = true,
            UseCurrentUserToken = false
        };

        process.Start();

        Log.Information("Started a new instance of the host with options: {@Options}", process.StartInfo);
    }

    private Process[] FindHostProcesses()
    {
        var processName = Path.GetFileNameWithoutExtension(_currentExecutablePath);

        return Process.GetProcessesByName(processName);
    }

    private bool IsUserInstance(Process process)
    {
        var commandLine = process.GetCommandLine();
        var expectedArgument = $"--launch-mode={_launchMode.ToString().ToLower()}";

        return commandLine.Contains(expectedArgument);
    }
}
