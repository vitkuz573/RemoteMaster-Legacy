using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Services;

public class ViewerStore : IViewerStore
{
    private readonly ConcurrentDictionary<string, Viewer> _viewers = new();

    public IReadOnlyDictionary<string, Viewer> Viewers => _viewers;

    public Viewer GetViewer(string connectionId)
    {
        if (_viewers.TryGetValue(connectionId, out var viewer))
        {
            return viewer;
        }

        throw new KeyNotFoundException($"Viewer with connection ID {connectionId} was not found.");
    }

    public void AddViewer(Viewer viewer)
    {
        _viewers.TryAdd(viewer.ConnectionId, viewer);
    }

    public bool RemoveViewer(string connectionId)
    {
        return _viewers.TryRemove(connectionId, out _);
    }
}
