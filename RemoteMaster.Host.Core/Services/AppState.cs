// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Host.Core.Resources;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Core.Services;

public class AppState(IHubContext<ControlHub, IControlClient> hubContext, ITrayIconManager trayIconManager, ILogger<AppState> logger) : IAppState
{
    private static readonly Lock EventLock = new();

    private readonly HashSet<string> _ignoredUsers = ["RCHost"];
    private readonly ConcurrentDictionary<string, IViewer> _viewers = new();
    private readonly ConcurrentDictionary<string, ICapturingContext> _capturingContexts = new();
    
    public event EventHandler<IViewer>? ViewerAdded;
    public event EventHandler<IViewer?>? ViewerRemoved;

    public event EventHandler<ICapturingContext>? CapturingContextAdded;
    public event EventHandler<ICapturingContext?>? CapturingContextRemoved;

    public IReadOnlyDictionary<string, IViewer> Viewers => _viewers.AsReadOnly();

    public IReadOnlyDictionary<string, ICapturingContext> CapturingContexts => _capturingContexts.AsReadOnly();

    public bool TryGetViewer(string connectionId, out IViewer? viewer)
    {
        return _viewers.TryGetValue(connectionId, out viewer);
    }

    public bool TryAddViewer(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        var result = _viewers.TryAdd(viewer.ConnectionId, viewer);

        if (result)
        {
            using (EventLock.EnterScope())
            {
                ViewerAdded?.Invoke(this, viewer);
            }

            NotifyViewersChanged();
            UpdateTrayIcon();
        }
        else
        {
            logger.LogError("Failed to add viewer with connection ID {ConnectionId}.", viewer.ConnectionId);
        }

        return result;
    }

    public bool TryRemoveViewer(string connectionId)
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

            NotifyViewersChanged();
            UpdateTrayIcon();
        }
        finally
        {
            viewer?.Dispose();
        }

        logger.LogInformation("Viewer with connection ID {ConnectionId} removed successfully.", connectionId);

        return true;
    }

    public IReadOnlyList<IViewer> GetAllViewers() => [.. _viewers.Values];

    public bool TryGetCapturingContext(string connectionId, out ICapturingContext? capturingContext)
    {
        return _capturingContexts.TryGetValue(connectionId, out capturingContext);
    }

    public bool TryAddCapturingContext(ICapturingContext capturingContext)
    {
        ArgumentNullException.ThrowIfNull(capturingContext);

        var result = _capturingContexts.TryAdd(capturingContext.ConnectionId, capturingContext);

        if (result)
        {
            using (EventLock.EnterScope())
            {
                CapturingContextAdded?.Invoke(this, capturingContext);
            }
        }
        else
        {
            logger.LogError("Failed to add capturing context with connection ID {ConnectionId}.", capturingContext.ConnectionId);
        }

        return result;
    }

    public bool TryRemoveCapturingContext(string connectionId)
    {
        if (!_capturingContexts.TryRemove(connectionId, out var capturingContext))
        {
            logger.LogError("Failed to remove capturing context with connection ID {ConnectionId}.", connectionId);

            return false;
        }

        try
        {
            using (EventLock.EnterScope())
            {
                CapturingContextRemoved?.Invoke(this, capturingContext);
            }
        }
        finally
        {
            capturingContext.Dispose();
        }

        logger.LogInformation("Capturing context with connection ID {ConnectionId} removed successfully.", connectionId);

        return true;
    }

    private void NotifyViewersChanged()
    {
        var viewers = GetAllViewers().Select(v => new ViewerDto(v.ConnectionId, v.Group, v.UserName, v.Role, v.ConnectedTime, v.IpAddress, v.AuthenticationType)).ToList();

        hubContext.Clients.All.ReceiveAllViewers(viewers);

        var activeConnections = viewers.Count(v => !_ignoredUsers.Contains(v.UserName));

        trayIconManager.UpdateConnectionCount(activeConnections);
    }

    private void UpdateTrayIcon()
    {
        var activeConnections = _viewers.Values.Count(v => !_ignoredUsers.Contains(v.UserName));
        var icon = activeConnections > 0
            ? Icons.with_connections
            : Icons.without_connections;

        trayIconManager.UpdateIcon(icon);
    }
}
