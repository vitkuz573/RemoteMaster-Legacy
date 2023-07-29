namespace RemoteMaster.Server.Core.Abstractions;

public interface IScreenCaster
{
    Task StartStreaming(string connectionId, CancellationToken cancellationToken);

    void SetSelectedScreen(string connectionId, string displayName);
}
