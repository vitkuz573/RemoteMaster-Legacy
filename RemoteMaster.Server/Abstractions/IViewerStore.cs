using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Abstractions;

public interface IViewerStore
{
    Viewer GetViewer(string connectionId);

    void AddViewer(string connectionId, Viewer viewer);

    void RemoveViewer(string connectionId);
}
