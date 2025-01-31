// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Services;

public class UserInstanceService : IUserInstanceService
{
    private const string Command = "user";

    private readonly string _currentExecutablePath = Environment.ProcessPath!;

    private readonly IInstanceManagerService _instanceManagerService;
    private readonly IProcessService _processService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<UserInstanceService> _logger;

    public UserInstanceService(IInstanceManagerService instanceManagerService, IProcessService processService, IFileSystem fileSystem, ILogger<UserInstanceService> logger)
    {
        _instanceManagerService = instanceManagerService;
        _processService = processService;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    public bool IsRunning => _processService
        .FindProcessesByName(_fileSystem.Path.GetFileName(_currentExecutablePath))
        .Any(p => _processService.HasProcessArgument(p, Command));

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
        var processes = _processService.FindProcessesByName(_fileSystem.Path.GetFileNameWithoutExtension(_currentExecutablePath));

        foreach (var process in processes)
        {
            if (!_processService.HasProcessArgument(process, Command))
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

        return _instanceManagerService.StartNewInstance(null, Command, [], startInfo);
    }
}
