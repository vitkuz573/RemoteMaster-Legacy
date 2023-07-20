using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Abstractions;

public interface IViewerStore
{
    Viewer GetViewer(string connectionId);

    void AddViewer(Viewer viewer);

    void RemoveViewer(string connectionId);
}
