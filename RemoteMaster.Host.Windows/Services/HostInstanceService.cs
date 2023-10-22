// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Management;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class HostInstanceService : IHostInstanceService
{
    private readonly ILogger<HostInstanceService> _logger;

    private static string CurrentExecutablePath => Environment.ProcessPath!;
    private const string InstanceArgument = "--user-instance";

    public HostInstanceService(ILogger<HostInstanceService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning()
    {
        return FindHostProcesses().Any(IsUserInstance);
    }

    public void Start()
    {
        try
        {
            StartNewInstance();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new instance of the host");
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
            _logger.LogInformation("{Message}", ex.Message);
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
            UseCurrentUserToken = false
        };

        NativeProcess.Start(options);
    }

    private static IEnumerable<Process> FindHostProcesses()
    {
        return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CurrentExecutablePath));
    }

    private bool IsUserInstance(Process process)
    {
        var commandLine = GetCommandLineOfProcess(process.Id);

        return commandLine != null && commandLine.Contains(InstanceArgument);
    }

    private static string GetCommandLineOfProcess(int processId)
    {
        var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}";

        using var searcher = new ManagementObjectSearcher(query);
        using var objects = searcher.Get();

        return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
    }
}
