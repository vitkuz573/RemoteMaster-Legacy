// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class ComputerConnectivityService(IAccessTokenProvider accessTokenProvider) : IComputerConnectivityService
{
    public async Task<bool> IsHubAvailable(Computer computer, string hubPath)
    {
        ArgumentNullException.ThrowIfNull(computer);

        try
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"https://{computer.IpAddress}:5001/{hubPath}", options =>
                {
                    options.AccessTokenProvider = async () => await accessTokenProvider.GetAccessTokenAsync();
                })
                .AddMessagePackProtocol()
                .Build();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            await connection.StartAsync(cts.Token);
            await connection.DisposeAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }
}

