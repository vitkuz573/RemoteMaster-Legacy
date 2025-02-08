// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ChatInstanceService(IInstanceManagerService instanceManagerService, IFileSystem fileSystem, IProcessService processService, ILogger<ChatInstanceService> logger) : IChatInstanceService
{
    private const string Command = "chat";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;

    public bool IsRunning => processService
        .GetProcessesByName(fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath))
        .Any(p => processService.HasProcessArgument(p, Command));

    public void Start()
    {
        try
        {
            StartNewInstance();

            logger.LogInformation("Successfully started a new {Command} instance of the host.", Command);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting new {Command} instance of the host. Executable path: {Path}", Command, _currentExecutablePath);
        }
    }

    public void Stop()
    {
        var processes = processService.GetProcessesByName(fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath));

        foreach (var process in processes)
        {
            if (!processService.HasProcessArgument(process, Command))
            {
                continue;
            }

            try
            {
                logger.LogInformation("Attempting to kill {Command} instance with ID: {ProcessId}.", Command, process.Id);
                process.Kill();
                logger.LogInformation("Successfully stopped an {Command} instance of the host. Process ID: {ProcessId}", Command, process.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping {Command} instance of the host. Process ID: {ProcessId}. Message: {Message}", Command, process.Id, ex.Message);
            }
        }
    }

    public void Restart()
    {
        Stop();

        while (IsRunning)
        {
            Task.Delay(50).Wait();
        }

        Start();
    }

    private int StartNewInstance()
    {
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true
        };

        return instanceManagerService.StartNewInstance(null, Command, [], startInfo);
    }
}
