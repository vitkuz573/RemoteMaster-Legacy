// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.EventArguments;
using RemoteMaster.Host.Core.Extensions;
using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Helpers;

namespace RemoteMaster.Host.Linux.Services;

public class UserInstanceService : IUserInstanceService
{
    private const string Command = "user";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;

    private readonly IEnvironmentProvider _environmentProvider;
    private readonly IInstanceManagerService _instanceManagerService;
    private readonly IProcessService _processService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<UserInstanceService> _logger;

    public UserInstanceService(ISessionChangeEventService sessionChangeEventService, IEnvironmentProvider environmentProvider, IInstanceManagerService instanceManagerService, IProcessService processService, IFileSystem fileSystem, ILogger<UserInstanceService> logger)
    {
        ArgumentNullException.ThrowIfNull(sessionChangeEventService);

        _environmentProvider = environmentProvider;
        _instanceManagerService = instanceManagerService;
        _processService = processService;
        _fileSystem = fileSystem;
        _logger = logger;

        sessionChangeEventService.SessionChanged += OnSessionChanged;
    }

    public bool IsRunning => _processService
        .GetProcessesByName(_fileSystem.Path.GetFileName(_currentExecutablePath))
        .Any(p => p.HasArgument(Command));

    public void Start()
    {
        try
        {
            var processId = StartNewInstance();

            _logger.LogInformation("Successfully started a new {Command} instance of the host.", Command);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error starting new {Command} instance of the host. Executable path: {Path}", Command, _currentExecutablePath);
        }
    }

    public void Stop()
    {
        var processes = _processService.GetProcessesByName(_fileSystem.Path.GetFileName(_currentExecutablePath));

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

        startInfo.Environment.Add("DISPLAY", _environmentProvider.GetDisplay());
        startInfo.Environment.Add("XAUTHORITY", _environmentProvider.GetXAuthority());

        return _instanceManagerService.StartNewInstance(null, Command, [], startInfo);
    }

    private async Task<bool> IsProcessEnvironmentValid(IProcess process)
    {
        await Task.Delay(5000);

        var expectedDisplay = _environmentProvider.GetDisplay();
        var expectedXAuthority = _environmentProvider.GetXAuthority();

        try
        {
            var envFilePath = $"/proc/{process.Id}/environ";

            if (!_fileSystem.File.Exists(envFilePath))
            {
                _logger.LogWarning("Environment file {EnvFilePath} not found for process {ProcessId}.", envFilePath, process.Id);

                return false;
            }

            var envBytes = await _fileSystem.File.ReadAllBytesAsync(envFilePath);
            var envText = Encoding.UTF8.GetString(envBytes);
            var envVars = envText.Split('\0', StringSplitOptions.RemoveEmptyEntries);

            string? actualDisplay = null;
            string? actualXAuthority = null;

            foreach (var envVar in envVars)
            {
                if (envVar.StartsWith("DISPLAY="))
                {
                    actualDisplay = envVar["DISPLAY=".Length..];
                }
                else if (envVar.StartsWith("XAUTHORITY="))
                {
                    actualXAuthority = envVar["XAUTHORITY=".Length..];
                }
            }

            if (actualDisplay == expectedDisplay && actualXAuthority == expectedXAuthority)
            {
                return true;
            }

            _logger.LogWarning("Process {ProcessId} has invalid environment. Expected: DISPLAY={ExpectedDisplay}, XAUTHORITY={ExpectedXAuthority}. Actual: DISPLAY={ActualDisplay}, XAUTHORITY={ActualXAuthority}.", process.Id, expectedDisplay, expectedXAuthority, actualDisplay, actualXAuthority);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking environment for process {ProcessId}.", process.Id);

            return false;
        }
    }

    private async void OnSessionChanged(object? sender, SessionChangeEventArgs e)
    {
        _logger.LogInformation("Session change detected. Restarting user instance...");

        Restart();

        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            var processes = _processService
                .GetProcessesByName(_fileSystem.Path.GetFileName(_currentExecutablePath))
                .Where(p => p.HasArgument(Command))
                .ToList();

            if (!processes.Any())
            {
                _logger.LogWarning("User instance not found. Restarting...");

                Restart();

                continue;
            }

            var validationTasks = processes.Select(IsProcessEnvironmentValid);
            var results = await Task.WhenAll(validationTasks);

            if (results.Any(valid => valid))
            {
                _logger.LogInformation("User instance environment is valid.");

                break;
            }

            _logger.LogWarning("User instance environment is invalid. Restarting...");

            Restart();
        }
    }
}
