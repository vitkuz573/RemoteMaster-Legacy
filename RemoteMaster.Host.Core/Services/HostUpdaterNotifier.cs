// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Models;
using static RemoteMaster.Shared.Models.Message;

namespace RemoteMaster.Host.Core.Services;

public class HostUpdaterNotifier(IHubContext<UpdaterHub, IUpdaterClient> hubContext, ILogger<HostUpdaterNotifier> logger) : IHostUpdaterNotifier
{
    /// <inheritdoc/>
    public async Task NotifyAsync(string message, MessageSeverity severity, string? meta = null)
    {
        var logLevel = severity switch
        {
            MessageSeverity.Information => LogLevel.Information,
            MessageSeverity.Warning => LogLevel.Warning,
            MessageSeverity.Error => LogLevel.Error,
            _ => LogLevel.Information
        };

        logger.Log(logLevel, "{Message}", message);

        using var reader = new StringReader(message);

        while (await reader.ReadLineAsync() is { } line)
        {
            var msg = new Message(line, severity)
            {
                Meta = meta
            };

            await hubContext.Clients.All.ReceiveMessage(msg);
        }
    }
}
