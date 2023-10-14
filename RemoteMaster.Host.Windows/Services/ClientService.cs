// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Services;

namespace RemoteMaster.Host.Services;

public class ClientService : IClientService
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Client";
    private const string Path = "C:\\Program Files\\RemoteMaster\\Client\\RemoteMaster.Host.exe";

    private readonly ILogger<ClientService> _logger;

    public ClientService(ILogger<ClientService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning()
    {
        if (Process.GetProcessesByName($"{MainAppName}.{SubAppName}").Any())
        {
            return true;
        }

        return false;
    }

    private static bool IsDomainJoined()
    {
        using var context = new PrincipalContext(ContextType.Machine);

        return context.ConnectedServer != null;
    }

    public void Start()
    {
        try
        {
            var options = new ProcessStartOptions(Path, -1)
            {
                ForceConsoleSession = true,
                DesktopName = "default",
                HiddenWindow = true,
                UseCurrentUserToken = false
            };

            using var _ = NativeProcess.Start(options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting {MainAppName} {SubAppName}", MainAppName, SubAppName);
        }
    }

    public void Stop()
    {
        foreach (var process in Process.GetProcessesByName($"{MainAppName}.{SubAppName}"))
        {
            process.Kill();
        }
    }
}
