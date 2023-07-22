namespace RemoteMaster.Server.Abstractions;

public interface IIdleTimer
{
    DateTime LastSeen { get; }

    void StartMonitoring();
}
