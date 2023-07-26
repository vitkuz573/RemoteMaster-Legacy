using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Abstractions;

public interface IViewerStore
{
    IReadOnlyDictionary<string, Viewer> Viewers { get; }

    bool TryGetViewer(string connectionId, out Viewer viewer);

    bool TryAddViewer(Viewer viewer);

    bool TryRemoveViewer(string connectionId);
}
