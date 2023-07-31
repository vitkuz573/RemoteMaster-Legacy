namespace RemoteMaster.Server.Core.Abstractions;

public interface IAppState
{
    event EventHandler<IViewer> ViewerAdded;

    event EventHandler<IViewer> ViewerRemoved;

    IReadOnlyDictionary<string, IViewer> Viewers { get; }

    bool TryGetViewer(string connectionId, out IViewer viewer);

    bool TryAddViewer(IViewer viewer);

    bool TryRemoveViewer(string connectionId, out IViewer viewer);
}
