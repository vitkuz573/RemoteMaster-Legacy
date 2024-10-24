// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;

namespace RemoteMaster.Host.Windows.Services;

public class ChatInstanceService(IInstanceManagerService instanceManagerService, IProcessService processService, ILogger<ChatInstanceService> logger) : IChatInstanceService
{
    private const string Argument = "--launch-mode=chat";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;

    public bool IsRunning => processService.FindProcessesByName(Path.GetFileNameWithoutExtension(_currentExecutablePath)).Any(p => processService.HasProcessArgument(p, Argument));

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
