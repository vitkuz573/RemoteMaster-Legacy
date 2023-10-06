// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Agent.Services;

public class ClientService : IClientService
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Client";
    private const string Path = "C:\\Program Files\\RemoteMaster\\Client\\RemoteMaster.Client.exe";
    private const string CertificateThumbprint = "E0BD3A7C39AA4FC012A0F6CB3297B46D5D73210C";

    private readonly ISignatureService _signatureService;
    private readonly IProcessService _processService;
    private readonly ILogger<ClientService> _logger;

    public ClientService(IProcessService processService, ISignatureService signatureService, ILogger<ClientService> logger)
    {
        _signatureService = signatureService;
        _processService = processService;
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

    public void Start()
    {
        if (_signatureService.IsSignatureValid(Path, CertificateThumbprint))
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

                _processService.Start(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting {MainAppName} {SubAppName}", MainAppName, SubAppName);
            }
        }
        else
        {
            _logger.LogError("The {MainAppName} {SubAppName} appears to be tampered with or its digital signature is not valid.", MainAppName, SubAppName);
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
