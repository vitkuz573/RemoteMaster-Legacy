// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Services;

public class AppState(IHubContext<ControlHub, IControlClient> hubContext) : IAppState
{
    public event EventHandler<IViewer>? ViewerAdded;

    public event EventHandler<IViewer?>? ViewerRemoved;

    private readonly ConcurrentDictionary<string, IViewer> _viewers = new();

    public IReadOnlyDictionary<string, IViewer> Viewers => _viewers;

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
            ViewerAdded?.Invoke(this, viewer);
            NotifyViewersChanged();
        }

        return result;
    }

    public bool TryRemoveViewer(string connectionId)
    {
#pragma warning disable CA2000
        var result = _viewers.TryRemove(connectionId, out var viewer);
#pragma warning restore CA2000

        if (!result)
        {
            return result;
        }

        try
        {
            ViewerRemoved?.Invoke(this, viewer);
            NotifyViewersChanged();
        }
        finally
        {
            viewer?.Dispose();
        }

        return result;
    }

    public IReadOnlyList<IViewer> GetAllViewers()
    {
        return [.. _viewers.Values];
    }

    private void NotifyViewersChanged()
    {
        var viewers = GetAllViewers().Select(v => new ViewerDto(v.ConnectionId, v.Group, v.UserName, v.Role, v.ConnectedTime)).ToList();

        hubContext.Clients.All.ReceiveAllViewers(viewers);
    }
}
