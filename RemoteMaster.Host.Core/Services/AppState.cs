﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Services;

public class AppState(IHubContext<ControlHub, IControlClient> hubContext, ILogger<AppState> logger) : IAppState
{
    private static readonly Lock EventLock = new();

    private readonly ConcurrentDictionary<string, IViewer> _viewers = new();
    
    public event EventHandler<IViewer>? ViewerAdded;
    public event EventHandler<IViewer?>? ViewerRemoved;

    public bool TryGetViewer(string connectionId, out IViewer? viewer)
    {
        return _viewers.TryGetValue(connectionId, out viewer);
    }

    public async Task<bool> TryAddViewerAsync(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        var result = _viewers.TryAdd(viewer.ConnectionId, viewer);

        if (result)
        {
            using (EventLock.EnterScope())
            {
                ViewerAdded?.Invoke(this, viewer);
            }

            await NotifyViewersChangedAsync();
        }
        else
        {
            logger.LogError("Failed to add viewer with connection ID {ConnectionId}.", viewer.ConnectionId);
        }

        return result;
    }

    public async Task<bool> TryRemoveViewerAsync(string connectionId)
    {
        if (!_viewers.TryRemove(connectionId, out var viewer))
        {
            logger.LogError("Failed to remove viewer with connection ID {ConnectionId}.", connectionId);

            return false;
        }

        try
        {
            viewer.Context.Abort();

            using (EventLock.EnterScope())
            {
                ViewerRemoved?.Invoke(this, viewer);
            }

            await NotifyViewersChangedAsync();
        }
        finally
        {
            viewer.Dispose();
        }

        logger.LogInformation("Viewer with connection ID {ConnectionId} removed successfully.", connectionId);

        return true;
    }

    public IReadOnlyList<IViewer> GetAllViewers() => [.. _viewers.Values];

    private async Task NotifyViewersChangedAsync()
    {
        var viewers = GetAllViewers().Select(v => new ViewerDto(v.ConnectionId, v.Group, v.UserName, v.Role, v.ConnectedTime, v.IpAddress, v.AuthenticationType)).ToList();

        await hubContext.Clients.All.ReceiveAllViewers(viewers);
    }
}
