// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using RemoteMaster.Server.Core.Abstractions;

namespace RemoteMaster.Server.Core.Services;

public class AppState : IAppState
{
    public event EventHandler<IViewer> ViewerAdded;

    public event EventHandler<IViewer> ViewerRemoved;

    private readonly ConcurrentDictionary<string, IViewer> _viewers = new();

    public IReadOnlyDictionary<string, IViewer> Viewers => _viewers;

    public bool TryGetViewer(string connectionId, out IViewer viewer)
    {
        return _viewers.TryGetValue(connectionId, out viewer);
    }

    public bool TryAddViewer(IViewer viewer)
    {
        if (viewer == null)
        {
            throw new ArgumentNullException(nameof(viewer));
        }

        var result = _viewers.TryAdd(viewer.ConnectionId, viewer);

        if (result)
        {
            viewer.StartStreaming();
        }

        return result;
    }

    public bool TryRemoveViewer(string connectionId, out IViewer viewer)
    {
        var result = _viewers.TryRemove(connectionId, out viewer);

        if (result)
        {
            viewer.StopStreaming();
        }

        return result;
    }
}
