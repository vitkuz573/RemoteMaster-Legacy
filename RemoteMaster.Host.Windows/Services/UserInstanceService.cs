// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.EventArguments;
using RemoteMaster.Host.Windows.Models;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public sealed class UserInstanceService : IUserInstanceService
{
    private const string Argument = "--launch-mode=user";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;
    private readonly IInstanceManagerService _instanceManagerService;
    private readonly IProcessService _processService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<UserInstanceService> _logger;

    public event EventHandler<UserInstanceCreatedEventArgs>? UserInstanceCreated;

    public bool IsRunning => _processService.FindProcessesByName(_fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath)).Any(p => _processService.HasProcessArgument(p, Argument));

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

            _logger.LogInformation("Successfully started a new instance of the host.");

            OnUserInstanceCreated(new UserInstanceCreatedEventArgs(processId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new instance of the host. Executable path: {Path}", _currentExecutablePath);
        }
    }

    public void Stop()
    {
        var processes = _processService.FindProcessesByName(_fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath));

        foreach (var process in processes)
        {
            if (!_processService.HasProcessArgument(process, Argument))
            {
                continue;
            }

            try
            {
                _logger.LogInformation("Attempting to kill process with ID: {ProcessId}.", process.Id);
                process.Kill();
                _logger.LogInformation("Successfully stopped an instance of the host. Process ID: {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping instance of the host. Process ID: {ProcessId}. Message: {Message}", process.Id, ex.Message);
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
            Arguments = Argument,
            CreateNoWindow = true
        };

        var options = new NativeProcessOptions
        {
            ForceConsoleSession = true,
            DesktopName = "Default",
            UseCurrentUserToken = false
        };

        return _instanceManagerService.StartNewInstance(null, startInfo, options);
    }

    private void OnSessionChanged(object? sender, SessionChangeEventArgs e)
    {
        if (e.Reason != WTS_CONSOLE_CONNECT)
        {
            return;
        }

        Restart();
    }

    private void OnUserInstanceCreated(UserInstanceCreatedEventArgs e)
    {
        UserInstanceCreated?.Invoke(this, e);
    }
}
