// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Extensions;
using RemoteMaster.Host.Windows.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class UserInstanceService : IUserInstanceService
{
    private const string Argument = "--user-instance";
    private readonly string CurrentExecutablePath = Environment.ProcessPath!;

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
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", CurrentExecutablePath);
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
        var options = new NativeProcessStartInfo(CurrentExecutablePath, -1)
        {
            Arguments = Argument,
            ForceConsoleSession = true,
            DesktopName = "default",
            CreateNoWindow = false,
            UseCurrentUserToken = false,
            InheritHandles = false
        };

        NativeProcess.Start(options);
        Log.Information("Started a new instance of the host with options: {Options}", options);
    }

    private IEnumerable<Process> FindHostProcesses()
    {
        var processName = Path.GetFileNameWithoutExtension(CurrentExecutablePath);
        
        return Process.GetProcessesByName(processName);
    }

    private static bool IsUserInstance(Process process)
    {
        var commandLine = process.GetCommandLine();
        
        return commandLine != null && commandLine.Contains(Argument);
    }
}
