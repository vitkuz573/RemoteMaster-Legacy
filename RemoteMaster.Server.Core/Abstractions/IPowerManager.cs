namespace RemoteMaster.Server.Core.Abstractions;

public interface IPowerManager
{
    void Shutdown();

    void Reboot();
}
