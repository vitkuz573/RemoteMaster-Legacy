// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Abstractions;
using RemoteMaster.Host.Linux.Extensions;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Services;

public class SessionWatcherService(ISessionChangeEventService sessionChangeEventService, ILogger<SessionWatcherService> logger) : IHostedService, IDisposable
{
    private Connection? _connection;
    private ILoginManager? _loginManager;
    private IDisposable? _sessionNewSubscription;
    private IDisposable? _sessionRemovedSubscription;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _connection = new Connection(Address.System);

            await _connection.ConnectAsync();

            _loginManager = _connection.CreateProxy<ILoginManager>("org.freedesktop.login1", new ObjectPath("/org/freedesktop/login1"));

            _sessionNewSubscription = await _loginManager.WatchSessionNewAsync(args => OnSessionNew(args.sessionId, args.objectPath), OnError);
            _sessionRemovedSubscription = await _loginManager.WatchSessionRemovedAsync(args => OnSessionRemoved(args.sessionId, args.objectPath), OnError);

            logger.LogInformation("[SessionWatcherService] Started and subscribed to session signals.");
        }
        catch (Exception ex)
        {
            logger.LogError($"[SessionWatcherService] Error during start: {ex}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[SessionWatcherService] Stopping service.");

        Dispose();

        return Task.CompletedTask;
    }

    private async void OnSessionNew(string sessionId, ObjectPath objectPath)
    {
        logger.LogInformation($"[SessionWatcherService] New session detected: {sessionId}, ObjectPath: {objectPath}");

        if (_connection == null)
        {
            logger.LogError("[SessionWatcherService] DBus connection is null.");

            return;
        }

        try
        {
            var sessionProxy = _connection.CreateProxy<ILoginSession>("org.freedesktop.login1", objectPath);

            var userId = await sessionProxy.GetUserAsync();
            var userName = await sessionProxy.GetNameAsync();
            var sessionType = await sessionProxy.GetTypeAsync();
            var remoteHost = await sessionProxy.GetRemoteAsync();
            var ttyPath = await sessionProxy.GetTTYAsync();

            logger.LogInformation($"[SessionWatcherService] Session {sessionId} details:");
            logger.LogInformation($"  - User ID: {userId}");
            logger.LogInformation($"  - Username: {userName}");
            logger.LogInformation($"  - Session Type: {sessionType}");
            logger.LogInformation($"  - Remote Host: {remoteHost}");
            logger.LogInformation($"  - TTY: {ttyPath}");

            if (!remoteHost)
            {
                sessionChangeEventService.OnSessionChanged(0);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[SessionWatcherService] Failed to get details for session {sessionId}");
        }
    }

    private void OnSessionRemoved(string sessionId, ObjectPath objectPath)
    {
        logger.LogInformation($"[SessionWatcherService] Session removed: {sessionId}, ObjectPath: {objectPath}");

        // sessionChangeEventService.OnSessionChanged(0);
    }

    private void OnError(Exception ex)
    {
        logger.LogError($"[SessionWatcherService] Error receiving signal: {ex}");
    }

    public void Dispose()
    {
        _sessionNewSubscription?.Dispose();
        _sessionRemovedSubscription?.Dispose();
        _connection?.Dispose();
    }
}
