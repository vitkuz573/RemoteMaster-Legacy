namespace RemoteMaster.Server.Abstractions;

public interface IViewerMonitorService
{
    DateTime LastSeen { get; set; }

    void StartMonitoring();
}
