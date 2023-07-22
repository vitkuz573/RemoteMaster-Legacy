using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Services;

public class ViewerStore : IViewerStore
{
    private readonly ConcurrentDictionary<string, Viewer> _viewers = new();
    private readonly ILogger<ViewerStore> _logger;

    public ViewerStore(ILogger<ViewerStore> logger)
    {
        _logger = logger;
    }

    public IReadOnlyDictionary<string, Viewer> Viewers => _viewers;

    public Viewer GetViewer(string connectionId)
    {
        if (_viewers.TryGetValue(connectionId, out var viewer))
        {
            return viewer;
        }

        _logger.LogWarning("Viewer with connection ID {ConnectionId} was not found.", connectionId);
        throw new KeyNotFoundException($"Viewer with connection ID {connectionId} was not found.");
    }

    public void AddViewer(Viewer viewer)
    {
        if (viewer == null)
        {
            throw new ArgumentNullException(nameof(viewer));
        }

        if (!_viewers.TryAdd(viewer.ConnectionId, viewer))
        {
            _logger.LogWarning("Failed to add viewer with connection ID {ConnectionId}.", viewer.ConnectionId);
            throw new InvalidOperationException($"Could not add viewer with connection ID {viewer.ConnectionId}.");
        }

        _logger.LogInformation("Successfully added viewer with connection ID {ConnectionId}.", viewer.ConnectionId);
    }

    public void RemoveViewer(string connectionId)
    {
        if (!_viewers.TryRemove(connectionId, out _))
        {
            _logger.LogWarning("Failed to remove viewer with connection ID {ConnectionId}.", connectionId);
            throw new InvalidOperationException($"Could not remove viewer with connection ID {connectionId}.");
        }

        _logger.LogInformation("Successfully removed viewer with connection ID {ConnectionId}.", connectionId);
    }
}
