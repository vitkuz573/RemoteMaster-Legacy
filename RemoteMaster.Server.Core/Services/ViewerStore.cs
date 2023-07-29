using System.Collections.Concurrent;
using RemoteMaster.Server.Core.Abstractions;

namespace RemoteMaster.Server.Core.Services;

public class ViewerStore : IViewerStore
{
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

        return _viewers.TryAdd(viewer.ConnectionId, viewer);
    }

    public bool TryRemoveViewer(string connectionId)
    {
        return _viewers.TryRemove(connectionId, out _);
    }
}
