namespace RemoteMaster.Server.Core.Abstractions;

public interface IIdleTimer
{
    DateTime LastSeen { get; }

    void StartMonitoring();
}
