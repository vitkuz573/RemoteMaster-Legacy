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

public class UserInstanceService : IUserInstanceService
{
    private readonly string _argument = "--launch-mode=user";
    private readonly string _currentExecutablePath = Environment.ProcessPath!;
    private readonly IInstanceStarterService _instanceStarterService;
    private readonly IProcessFinderService _processFinderService;

    public event EventHandler<UserInstanceCreatedEventArgs>? UserInstanceCreated;

    public bool IsRunning => _processFinderService.FindHostProcesses(_currentExecutablePath).Any(p => _processFinderService.IsUserInstance(p, _argument));

    public UserInstanceService(ISessionChangeEventService sessionChangeEventService, IInstanceStarterService instanceStarterService, IProcessFinderService processFinderService)
    {
        ArgumentNullException.ThrowIfNull(sessionChangeEventService);

        _instanceStarterService = instanceStarterService;
        _processFinderService = processFinderService;

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
        foreach (var process in _processFinderService.FindHostProcesses(_currentExecutablePath))
        {
            if (!_processFinderService.IsUserInstance(process, _argument))
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

    private int StartNewInstance()
    {
        var startInfo = new NativeProcessStartInfo
        {
            Arguments = _argument,
            ForceConsoleSession = true,
            DesktopName = "Default",
            CreateNoWindow = true,
            UseCurrentUserToken = false
        };

        return _instanceStarterService.StartNewInstance(_currentExecutablePath, null, startInfo);
    }

    private void OnSessionChanged(object? sender, SessionChangeEventArgs e)
    {
        if (e.Reason == WTS_CONSOLE_CONNECT)
        {
            Stop();

            while (IsRunning)
            {
                Task.Delay(50).Wait();
            }

            Start();
        }
    }

    protected virtual void OnUserInstanceCreated(UserInstanceCreatedEventArgs e)
    {
        UserInstanceCreated?.Invoke(this, e);
    }
}
