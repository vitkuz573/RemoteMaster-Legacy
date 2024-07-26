// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Models;
using Serilog;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public sealed class UserInstanceService : IUserInstanceService
{
    private const string Argument = "--launch-mode=user";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;
    private readonly IInstanceStarterService _instanceStarterService;
    private readonly IProcessService _processService;

    public event EventHandler<UserInstanceCreatedEventArgs>? UserInstanceCreated;

    public bool IsRunning => _processService.FindProcessesByName(Path.GetFileNameWithoutExtension(_currentExecutablePath)).Any(p => _processService.HasProcessArgument(p, Argument));

    public UserInstanceService(ISessionChangeEventService sessionChangeEventService, IInstanceStarterService instanceStarterService, IProcessService processService)
    {
        ArgumentNullException.ThrowIfNull(sessionChangeEventService);

        _instanceStarterService = instanceStarterService;
        _processService = processService;

        sessionChangeEventService.SessionChanged += OnSessionChanged;
    }

    public void Start()
    {
        try
        {
            var processId = StartNewInstance();

            Log.Information("Successfully started a new instance of the host.");

            OnUserInstanceCreated(new UserInstanceCreatedEventArgs(processId));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting new instance of the host. Executable path: {Path}", _currentExecutablePath);
        }
    }

    public void Stop()
    {
        var processes = _processService.FindProcessesByName(Path.GetFileNameWithoutExtension(_currentExecutablePath));

        foreach (var process in processes)
        {
            if (!_processService.HasProcessArgument(process, Argument))
            {
                continue;
            }

            try
            {
                Log.Information("Attempting to kill process with ID: {ProcessId}.", process.Id);
                process.Kill();
                Log.Information("Successfully stopped an instance of the host. Process ID: {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error stopping instance of the host. Process ID: {ProcessId}. Message: {Message}", process.Id, ex.Message);
            }
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

        return _instanceStarterService.StartNewInstance(_currentExecutablePath, null, startInfo);
    }

    private void OnSessionChanged(object? sender, SessionChangeEventArgs e)
    {
        if (e.Reason != WTS_CONSOLE_CONNECT)
        {
            return;
        }

        Stop();

        while (IsRunning)
        {
            Task.Delay(50).Wait();
        }

        Start();
    }

    private void OnUserInstanceCreated(UserInstanceCreatedEventArgs e)
    {
        UserInstanceCreated?.Invoke(this, e);
    }
}
