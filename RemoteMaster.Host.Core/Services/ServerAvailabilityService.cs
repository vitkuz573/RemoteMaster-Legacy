// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ServerAvailabilityService(ITcpClientFactory tcpClientFactory, ITimeProvider timeProvider, ILogger<ServerAvailabilityService> logger) : IServerAvailabilityService
{
    public async Task<bool> IsServerAvailableAsync(string server, int maxAttempts, int initialRetryDelay, int maxRetryDelay, CancellationToken cancellationToken = default)
    {
        var currentRetryDelay = initialRetryDelay;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                logger.LogInformation("Checking server availability, attempt {Attempt} of {MaxAttempts}...", attempt, maxAttempts);

                using var tcpClient = tcpClientFactory.Create();
                await tcpClient.ConnectAsync(server, 5254, cancellationToken);

                logger.LogInformation("Server {Server} is available.", server);

                return true;
            }
            catch (SocketException)
            {
                if (attempt == maxAttempts)
                {
                    break;
                }

                logger.LogWarning("Attempt {Attempt} failed due to socket error. Retrying in {RetryDelay}ms...", attempt, currentRetryDelay);

                await timeProvider.Delay(currentRetryDelay, cancellationToken);

                currentRetryDelay = Math.Min(currentRetryDelay * 2, maxRetryDelay);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Operation was canceled by the user.");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while checking server availability.");
                break;
            }
        }

        if (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Server {Server} is unavailable after {MaxAttempts} attempts.", server, maxAttempts);
        }

        return false;
    }
}
