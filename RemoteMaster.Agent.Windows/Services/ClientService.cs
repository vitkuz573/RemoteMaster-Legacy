// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteMaster.Agent.Abstractions;
using RemoteMaster.Agent.Core.Abstractions;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Agent.Services;

public class ClientService : IClientService
{
    private readonly ISignatureService _signatureService;
    private readonly ILogger<ClientService> _logger;

    private const string ClientPath = "C:\\Program Files\\RemoteMaster\\Client\\RemoteMaster.Client.exe";
    private const string CertificateThumbprint = "E0BD3A7C39AA4FC012A0F6CB3297B46D5D73210C";

    public ClientService(ISignatureService signatureService, ILogger<ClientService> logger)
    {
        _signatureService = signatureService;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(ConfigurationModel config, string hostName, string ipAddress)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Installing...");
            var result = await connection.InvokeAsync<bool>("RegisterClient", hostName, ipAddress, config.Group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Installation failed: {ex.Message}");

            return false;
        }
    }

    public async Task<bool> UnregisterAsync(ConfigurationModel config, string hostName)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            var connection = await ConnectToServerHub($"http://{config.Server}:5254");

            _logger.LogInformation("Uninstalling...");
            var result = await connection.InvokeAsync<bool>("UnregisterClient", hostName, config.Group);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Uninstallation failed: {ex.Message}");

            return false;
        }
    }

    private static async Task<HubConnection> ConnectToServerHub(string serverUrl)
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/hubs/management")
            .Build();

        await hubConnection.StartAsync();

        return hubConnection;
    }

    public bool IsClientRunning()
    {
        var clientFullPath = Path.GetFullPath(ClientPath);

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
                _logger.LogError(ex, $"Unable to enumerate the process modules for process ID: {process.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occurred when handling process ID: {process.Id}");
            }
        }

        return false;
    }

    public void StartClient()
    {
        if (_signatureService.IsSignatureValid(ClientPath, CertificateThumbprint))
        {
            try
            {
                ProcessHelper.OpenInteractiveProcess(ClientPath, -1, true, "default", true, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting RemoteMaster Client");
            }
        }
        else
        {
            _logger.LogError("The RemoteMaster client appears to be tampered with or its digital signature is not valid.");
        }
    }
}
