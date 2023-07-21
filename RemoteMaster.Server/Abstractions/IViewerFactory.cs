using RemoteMaster.Server.Services;

namespace RemoteMaster.Server.Abstractions;

public interface IViewerFactory
{
    Viewer Create(string connectionId);
}