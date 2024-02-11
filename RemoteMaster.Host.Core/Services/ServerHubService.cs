// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class ServerHubService : IServerHubService
{
    private HubConnection _hubConnection = null!;

    public async Task ConnectAsync(string serverIp)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"http://{serverIp}:5254/hubs/management")
            .AddMessagePackProtocol()
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task<string> GetNewOrganizationalUnitIfChangeRequested(string macAddress)
    {
        return await _hubConnection.InvokeAsync<string>("GetNewOrganizationalUnitIfChangeRequested", macAddress);
    }

    public async Task AcknowledgeOrganizationalUnitChange(string macAddress)
    {
        await _hubConnection.InvokeAsync("AcknowledgeOrganizationalUnitChange", macAddress);
    }

    public void OnReceiveCertificate(Action<byte[]> onReceiveCertificate)
    {
        _hubConnection.On<byte[]>("ReceiveCertificate", onReceiveCertificate);
    }

    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration, byte[] signingRequest)
    {
        return await _hubConnection.InvokeAsync<bool>("RegisterHostAsync", hostConfiguration, signingRequest);
    }

    public async Task<bool> UnregisterHostAsync(HostConfiguration hostConfiguration)
    {
        return await _hubConnection.InvokeAsync<bool>("UnregisterHostAsync", hostConfiguration);
    }

    public async Task<bool> UpdateHostInformationAsync(HostConfiguration hostConfiguration)
    {
        return await _hubConnection.InvokeAsync<bool>("UpdateHostInformationAsync", hostConfiguration);
    }

    public async Task<bool> IsHostRegisteredAsync(HostConfiguration hostConfiguration)
    {
        return await _hubConnection.InvokeAsync<bool>("IsHostRegisteredAsync", hostConfiguration);
    }

    public async Task<string> GetPublicKeyAsync()
    {
        return await _hubConnection.InvokeAsync<string>("GetPublicKey");
    }
}
