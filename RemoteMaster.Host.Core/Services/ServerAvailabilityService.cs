// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ServerAvailabilityService(ITcpClientFactory tcpClientFactory, ITimeProvider timeProvider, ILogger<ServerAvailabilityService> logger) : IServerAvailabilityService
{
    public const int MaxConnectionAttempts = 5;
    public const int ConnectionRetryDelay = 1000;
    private const int MaxRetryDelay = 5000;

    public async Task<bool> IsServerAvailableAsync(string server, CancellationToken cancellationToken = default)
    {
        var currentRetryDelay = ConnectionRetryDelay;

        for (var attempt = 1; attempt <= MaxConnectionAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation("Checking server availability, attempt {Attempt} of {MaxAttempts}...", attempt, MaxConnectionAttempts);

                using var tcpClient = tcpClientFactory.Create();
                await tcpClient.ConnectAsync(server, 5254, cancellationToken);

                logger.LogInformation("Server {Server} is available.", server);

                return true;
            }
            catch (SocketException)
            {
                logger.LogWarning("Attempt {Attempt} failed due to socket error. Retrying in {RetryDelay}ms...", attempt, currentRetryDelay);

                if (attempt == MaxConnectionAttempts)
                {
                    break;
                }

                await timeProvider.Delay(currentRetryDelay, cancellationToken);

                currentRetryDelay = Math.Min(currentRetryDelay * 2, MaxRetryDelay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while checking server availability.");
                break;
            }
        }

        logger.LogError("Server {Server} is unavailable after {MaxAttempts} attempts.", server, MaxConnectionAttempts);

        return false;
    }
}
