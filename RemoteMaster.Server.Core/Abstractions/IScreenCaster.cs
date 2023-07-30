namespace RemoteMaster.Server.Core.Abstractions;

public interface IScreenCaster
{
    Task StartStreaming(string connectionId);

    void StopStreaming(string connectionId);

    void SetSelectedScreen(string connectionId, string displayName);
}
