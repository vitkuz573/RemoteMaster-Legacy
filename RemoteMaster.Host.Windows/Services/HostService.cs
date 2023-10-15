// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.Management;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Host.Services;

public class HostService : IHostService
{
    private readonly ILogger<HostService> _logger;

    private static string CurrentExecutablePath => Environment.ProcessPath!;
    private const string InstanceArgument = "--user-instance";

    public HostService(ILogger<HostService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning()
    {
        return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CurrentExecutablePath))
                      .Any(p => GetCommandLineOfProcess(p.Id)?.Contains(InstanceArgument) ?? false);
    }

    public void Start()
    {
        try
        {
            var options = new ProcessStartOptions(CurrentExecutablePath, -1)
            {
                Arguments = InstanceArgument,
                ForceConsoleSession = true,
                DesktopName = "default",
                HiddenWindow = false,
                UseCurrentUserToken = false
            };

            using var _ = NativeProcess.Start(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new instance of the host");
        }
    }

    public string GetCommandLineOfProcess(int processId)
    {
        var query = $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}";
        
        using var searcher = new ManagementObjectSearcher(query);
        using var objects = searcher.Get();
        
        var commandLine = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
        
        return commandLine;
    }

    public void Stop()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(CurrentExecutablePath)))
            {
                var commandLine = GetCommandLineOfProcess(process.Id);
                
                if (commandLine != null && commandLine.Contains(InstanceArgument))
                {
                    process.Kill();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }
    }
}
