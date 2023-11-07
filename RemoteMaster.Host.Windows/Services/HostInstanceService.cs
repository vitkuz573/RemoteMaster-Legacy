// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Extensions;
using RemoteMaster.Host.Windows.Models;
using Serilog;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstanceService : IHostInstanceService
{
    private static string CurrentExecutablePath => Environment.ProcessPath!;
    private const string InstanceArgument = "--user-instance";

    public bool IsRunning() => FindHostProcesses().Any(IsUserInstance);

    public void Start()
    {
        try
        {
            StartNewInstance();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host");
        }
    }

    public void Stop()
    {
        try
        {
            foreach (var process in FindHostProcesses())
            {
                if (IsUserInstance(process))
                {
                    process.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Information("{Message}", ex.Message);
        }
    }

    private static void StartNewInstance()
    {
        var options = new NativeProcessStartInfo(CurrentExecutablePath, -1)
        {
            Arguments = InstanceArgument,
            ForceConsoleSession = true,
            DesktopName = "default",
            CreateNoWindow = false,
            UseCurrentUserToken = false,
            InheritHandles = false
        };

        NativeProcess.Start(options);
    }

    private static IEnumerable<Process> FindHostProcesses()
    {
        var processName = Path.GetFileNameWithoutExtension(CurrentExecutablePath);

        return Process.GetProcessesByName(processName);
    }

    private bool IsUserInstance(Process process)
    {
        var commandLine = process.GetCommandLine();

        return commandLine != null && commandLine.Contains(InstanceArgument);
    }
}
