// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Linux.Abstractions;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Services;

public class SessionWatcherService(ILogger<SessionWatcherService> logger) : IHostedService, IDisposable
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

            _sessionNewSubscription = await _loginManager.WatchSessionNewAsync(args => OnSessionNew(args.sessionId, args.sessionPath), OnError);
            _sessionRemovedSubscription = await _loginManager.WatchSessionRemovedAsync(args => OnSessionRemoved(args.sessionId, args.sessionPath), OnError);

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

    private void OnSessionNew(string sessionId, ObjectPath sessionPath)
    {
        logger.LogInformation($"[SessionWatcherService] New session detected: {sessionId}, ObjectPath: {sessionPath}");
    }

    private void OnSessionRemoved(string sessionId, ObjectPath sessionPath)
    {
        logger.LogInformation($"[SessionWatcherService] Session removed: {sessionId}, ObjectPath: {sessionPath}");
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
