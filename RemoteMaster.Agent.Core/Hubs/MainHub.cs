// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Agent.Core.Models;

namespace RemoteMaster.Agent.Core.Hubs;

public class MainHub : Hub
{
    private readonly ISignatureService _signatureService;
    private readonly IProcessService _processService;
    private readonly IOptions<ServerSettings> _settings;
    private readonly ILogger<MainHub> _logger;

    public MainHub(ISignatureService signatureService, IProcessService processService, IOptions<ServerSettings> settings, ILogger<MainHub> logger)
    {
        _signatureService = signatureService;
        _processService = processService;
        _settings = settings;
        _logger = logger;
    }

    public async override Task OnConnectedAsync()
    {
        if (!IsServerRunning())
        {            
            if (_signatureService.IsSignatureValid(_settings.Value.Path, _settings.Value.CertificateThumbprint))
            {
                try
                {
                    _processService.Start(_settings.Value.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error starting RemoteMaster Server");
                }
            }
            else
            {
                await Clients.Caller.SendAsync("ServerTampered", "The RemoteMaster server appears to be tampered with or its digital signature is not valid. Please contact support.");
            }
        }

        await base.OnConnectedAsync();
    }

    private bool IsServerRunning() => Process.GetProcessesByName(Path.GetFileNameWithoutExtension(_settings.Value.Path)).Any();
}
