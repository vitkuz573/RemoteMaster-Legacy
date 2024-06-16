// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class ServerHubService : IServerHubService
{
    private HubConnection _hubConnection = null!;

    public async Task ConnectAsync(string server)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"http://{server}:5254/hubs/management")
            .Build();

        await _hubConnection.StartAsync();
    }

    public async Task<HostMoveRequest?> GetHostMoveRequest(string macAddress)
    {
        return await _hubConnection.InvokeAsync<HostMoveRequest?>("GetHostMoveRequest", macAddress);
    }

    public async Task AcknowledgeMoveRequest(string macAddress)
    {
        await _hubConnection.InvokeAsync("AcknowledgeMoveRequest", macAddress);
    }

    public void OnReceiveCertificate(Action<byte[]> onReceiveCertificate)
    {
        _hubConnection.On("ReceiveCertificate", onReceiveCertificate);
    }

    public void OnReceiveHostGuid(Action<Guid> onReceiveHostGuid)
    {
        _hubConnection.On("ReceiveHostGuid", onReceiveHostGuid);
    }

    public async Task<bool> IssueCertificateAsync(byte[] signingRequest)
    {
        return await _hubConnection.InvokeAsync<bool>("IssueCertificateAsync", signingRequest);
    }

    public async Task<bool> RegisterHostAsync(HostConfiguration hostConfiguration)
    {
        return await _hubConnection.InvokeAsync<bool>("RegisterHostAsync", hostConfiguration);
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

    public async Task<byte[]> GetPublicKeyAsync()
    {
        return await _hubConnection.InvokeAsync<byte[]>("GetPublicKey");
    }

    public async Task<bool> GetCaCertificateAsync()
    {
        return await _hubConnection.InvokeAsync<bool>("GetCaCertificateAsync");
    }
}
