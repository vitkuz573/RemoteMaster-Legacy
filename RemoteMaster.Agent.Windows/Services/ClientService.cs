// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Agent.Services;

public class ClientService : IClientService
{
    private const string MainAppName = "RemoteMaster";
    private const string SubAppName = "Client";
    private const string Path = "C:\\Program Files\\RemoteMaster\\Client\\RemoteMaster.Client.exe";
    private const string CertificateThumbprint = "E0BD3A7C39AA4FC012A0F6CB3297B46D5D73210C";

    private readonly ISignatureService _signatureService;
    private readonly ILogger<ClientService> _logger;

    public ClientService(ISignatureService signatureService, ILogger<ClientService> logger)
    {
        _signatureService = signatureService;
        _logger = logger;
    }

    public bool IsRunning()
    {
        var clientFullPath = System.IO.Path.GetFullPath(Path);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (_signatureService.IsProcessSignatureValid(process, clientFullPath, CertificateThumbprint))
                {
                    return true;
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Unable to enumerate the process modules for process ID: {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred when handling process ID: {ProcessId}", process.Id);
            }
        }

        return false;
    }

    public void Start()
    {
        if (_signatureService.IsSignatureValid(Path, CertificateThumbprint))
        {
            try
            {
                ProcessHelper.OpenInteractiveProcess(Path, -1, true, "default", true, out _);
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
        var clientFullPath = System.IO.Path.GetFullPath(Path);

        foreach (var process in Process.GetProcessesByName($"{MainAppName}.{SubAppName}"))
        {
            try
            {
                if (_signatureService.IsProcessSignatureValid(process, clientFullPath, CertificateThumbprint))
                {
                    process.Kill();
                    _logger.LogInformation("{MainAppName} {SubAppName} stopped successfully.", MainAppName, SubAppName);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Unable to stop the process for process ID: {ProcessId}", process.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred when stopping process ID: {ProcessId}", process.Id);
            }
        }
    }
}
