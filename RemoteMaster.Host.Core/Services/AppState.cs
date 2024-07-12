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
    private readonly ConcurrentDictionary<string, IViewer> _viewers = new();
    private static readonly object Lock = new();

    public event EventHandler<IViewer>? ViewerAdded;
    public event EventHandler<IViewer?>? ViewerRemoved;

    public IReadOnlyDictionary<string, IViewer> Viewers
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, IViewer>(_viewers);
            }
        }
    }

    public bool TryGetViewer(string connectionId, out IViewer? viewer)
    {
        lock (Lock)
        {
            return _viewers.TryGetValue(connectionId, out viewer);
        }
    }

    public bool TryAddViewer(IViewer viewer)
    {
        lock (Lock)
        {
            ArgumentNullException.ThrowIfNull(viewer);

            var result = _viewers.TryAdd(viewer.ConnectionId, viewer);

            if (!result)
            {
                return result;
            }

            ViewerAdded?.Invoke(this, viewer);
            NotifyViewersChanged();

            return result;
        }
    }

    public bool TryRemoveViewer(string connectionId)
    {
        lock (Lock)
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
    }

    public IReadOnlyList<IViewer> GetAllViewers()
    {
        lock (Lock)
        {
            return [.. _viewers.Values];
        }
    }

    private void NotifyViewersChanged()
    {
        List<ViewerDto> viewers;

        lock (Lock)
        {
            viewers = GetAllViewers().Select(v => new ViewerDto(v.ConnectionId, v.Group, v.UserName, v.Role, v.ConnectedTime, v.IpAddress)).ToList();
        }

        hubContext.Clients.All.ReceiveAllViewers(viewers);
    }
}
