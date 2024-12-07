// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Services;

public class UpdaterInstanceService : IUpdaterInstanceService
{
    private readonly string _updaterExecutablePath;

    private readonly IInstanceManagerService _instanceManagerService;
    private readonly ILogger<UpdaterInstanceService> _logger;

    public UpdaterInstanceService(IInstanceManagerService instanceManagerService, IFileSystem fileSystem, ILogger<UpdaterInstanceService> logger)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        _instanceManagerService = instanceManagerService;
        _logger = logger;

        var currentExecutableName = fileSystem.Path.GetFileName(Environment.ProcessPath!);
        var baseFolderPath = fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RemoteMaster", "Host", "Updater");
        
        _updaterExecutablePath = fileSystem.Path.Combine(baseFolderPath, currentExecutableName);
    }

    public void Start(UpdateRequest updateRequest)
    {
        ArgumentNullException.ThrowIfNull(updateRequest);

        var arguments = new List<string>
        {
            "--folder-path",
            updateRequest.FolderPath
        };

        if (updateRequest.UserCredentials != null && !string.IsNullOrWhiteSpace(updateRequest.UserCredentials.UserName))
        {
            arguments.Add("--username");
            arguments.Add(updateRequest.UserCredentials.UserName);
        }

        if (updateRequest.UserCredentials != null && !string.IsNullOrWhiteSpace(updateRequest.UserCredentials.Password))
        {
            arguments.Add("--password");
            arguments.Add(updateRequest.UserCredentials.Password);
        }

        if (updateRequest.ForceUpdate)
        {
            arguments.Add("--force");
        }

        if (updateRequest.AllowDowngrade)
        {
            arguments.Add("--allow-downgrade");
        }

        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            _instanceManagerService.StartNewInstance(_updaterExecutablePath, "update", arguments.ToArray(), startInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new instance of the host. Executable path: {ExecutablePath}", _updaterExecutablePath);
        }
    }
}
