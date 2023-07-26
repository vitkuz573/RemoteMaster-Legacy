using System.Collections.Concurrent;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ViewerStore : IViewerStore
{
    private readonly ConcurrentDictionary<string, Viewer> _viewers = new();

    public IReadOnlyDictionary<string, Viewer> Viewers => _viewers;

    public bool TryGetViewer(string connectionId, out Viewer viewer)
    {
        return _viewers.TryGetValue(connectionId, out viewer);
    }

    public bool TryAddViewer(Viewer viewer)
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
