// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Models;
using Serilog;

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
                Log.Error("Failed to add viewer with connection ID {ConnectionId}.", viewer.ConnectionId);
                return result;
            }

            ViewerAdded?.Invoke(this, viewer);
            NotifyViewersChanged();

            Log.Information("Viewer with connection ID {ConnectionId} added successfully.", viewer.ConnectionId);

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
                Log.Error("Failed to remove viewer with connection ID {ConnectionId}.", connectionId);

                return result;
            }

            try
            {
                viewer?.Context.Abort();
                ViewerRemoved?.Invoke(this, viewer);
                NotifyViewersChanged();
            }
            finally
            {
                viewer?.Dispose();
            }

            Log.Information("Viewer with connection ID {ConnectionId} removed successfully.", connectionId);

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
            viewers = GetAllViewers().Select(v => new ViewerDto(v.ConnectionId, v.Group, v.UserName, v.Role, v.ConnectedTime, v.IpAddress, v.AuthenticationType)).ToList();
        }

        hubContext.Clients.All.ReceiveAllViewers(viewers);
    }
}
