using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Services;

public class ViewerStore : IViewerStore
{
    private readonly ConcurrentDictionary<string, Viewer> _viewers = new();

    public Viewer GetViewer(string connectionId)
    {
        _viewers.TryGetValue(connectionId, out var viewer);
        return viewer;
    }

    public void AddViewer(string connectionId, Viewer viewer)
    {
        _viewers.TryAdd(connectionId, viewer);
    }

    public void RemoveViewer(string connectionId)
    {
        _viewers.TryRemove(connectionId, out _);
    }
}
