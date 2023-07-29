namespace RemoteMaster.Server.Core.Abstractions;

public interface IViewerStore
{
    IReadOnlyDictionary<string, IViewer> Viewers { get; }

    bool TryGetViewer(string connectionId, out IViewer viewer);

    bool TryAddViewer(IViewer viewer);

    bool TryRemoveViewer(string connectionId);
}
