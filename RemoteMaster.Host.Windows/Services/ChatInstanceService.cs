// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class ChatInstanceService(IInstanceManagerService instanceManagerService, IFileSystem fileSystem, IProcessService processService, ILogger<ChatInstanceService> logger) : IChatInstanceService
{
    private const string Argument = "--launch-mode=chat";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;

    public bool IsRunning => processService.FindProcessesByName(fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath)).Any(p => processService.HasProcessArgument(p, Argument));

    public void Start()
    {
        try
        {
            StartNewInstance();

            logger.LogInformation("Successfully started a new chat instance of the host.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting new instance of the host. Executable path: {Path}", _currentExecutablePath);
        }
    }

    public void Stop()
    {
        var processes = processService.FindProcessesByName(fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath));

        foreach (var process in processes)
        {
            if (!processService.HasProcessArgument(process, Argument))
            {
                continue;
            }

            try
            {
                logger.LogInformation("Attempting to kill process with ID: {ProcessId}.", process.Id);
                process.Kill();
                logger.LogInformation("Successfully stopped an instance of the host. Process ID: {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping instance of the host. Process ID: {ProcessId}. Message: {Message}", process.Id, ex.Message);
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
        var startInfo = new NativeProcessStartInfo
        {
            Arguments = Argument,
            ForceConsoleSession = true,
            DesktopName = "Default",
            CreateNoWindow = true,
            UseCurrentUserToken = false
        };

        return instanceManagerService.StartNewInstance(null, startInfo);
    }
}
