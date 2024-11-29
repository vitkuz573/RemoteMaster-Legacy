// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ServerAvailabilityService(ILogger<ServerAvailabilityService> logger) : IServerAvailabilityService
{
    private const int MaxConnectionAttempts = 5;
    private const int ConnectionRetryDelay = 1000;

    public async Task<bool> IsServerAvailableAsync(string server)
    {
        for (var attempt = 1; attempt <= MaxConnectionAttempts; attempt++)
        {
            try
            {
                logger.LogInformation("Checking server availability, attempt {Attempt} of {MaxAttempts}...", attempt, MaxConnectionAttempts);

                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(server, 5254);

                logger.LogInformation("Server {Server} is available.", server);

                return true;
            }
            catch (SocketException)
            {
                logger.LogWarning("Attempt {Attempt} failed. Retrying in {RetryDelay}ms...", attempt, ConnectionRetryDelay);

                await Task.Delay(ConnectionRetryDelay);
            }
        }

        logger.LogError("Server {Server} is unavailable after {MaxAttempts} attempts.", server, MaxConnectionAttempts);

        return false;
    }
}
