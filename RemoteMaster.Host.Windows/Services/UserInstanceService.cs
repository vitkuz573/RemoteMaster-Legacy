// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.EventArguments;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Windows.Models;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public sealed class UserInstanceService : IUserInstanceService
{
    private const string Command = "user";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;
    private readonly IInstanceManagerService _instanceManagerService;
    private readonly IProcessService _processService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<UserInstanceService> _logger;

    public bool IsRunning => _processService
        .GetProcessesByName(_fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath))
        .Any(p => p.HasArgument(Command));

    public UserInstanceService(ISessionChangeEventService sessionChangeEventService, IInstanceManagerService instanceManagerService, IProcessService processService, IFileSystem fileSystem, ILogger<UserInstanceService> logger)
    {
        ArgumentNullException.ThrowIfNull(sessionChangeEventService);

        _instanceManagerService = instanceManagerService;
        _processService = processService;
        _fileSystem = fileSystem;
        _logger = logger;

        sessionChangeEventService.SessionChanged += OnSessionChanged;
    }

    public void Start()
    {
        try
        {
            var processId = StartNewInstance();

            _logger.LogInformation("Successfully started a new {Command} instance of the host.", Command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new {Command} instance of the host. Executable path: {Path}", Command, _currentExecutablePath);
        }
    }

    public void Stop()
    {
        var processes = _processService.GetProcessesByName(_fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath));

        foreach (var process in processes)
        {
            if (!process.HasArgument(Command))
            {
                continue;
            }

            try
            {
                _logger.LogInformation("Attempting to kill {Command} instance with ID: {ProcessId}.", Command, process.Id);
                process.Kill();
                _logger.LogInformation("Successfully stopped an {Command} instance of the host. Process ID: {ProcessId}", Command, process.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping {Command} instance of the host. Process ID: {ProcessId}. Message: {Message}", Command, process.Id, ex.Message);
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

        var options = new NativeProcessOptions
        {
            ForceConsoleSession = true,
            DesktopName = "Default",
            UseCurrentUserToken = false
        };

        return _instanceManagerService.StartNewInstance(null, Command, [], startInfo, options);
    }

    private void OnSessionChanged(object? sender, SessionChangeEventArgs e)
    {
        if (e.Reason != WTS_CONSOLE_CONNECT)
        {
            return;
        }

        Restart();
    }
}
