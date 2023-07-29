namespace RemoteMaster.Server.Core.Abstractions;

public interface IViewerFactory
{
    IViewer Create(string connectionId);
}