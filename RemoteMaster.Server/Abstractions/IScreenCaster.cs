namespace RemoteMaster.Server.Abstractions;

public interface IScreenCaster
{
    Task StartStreaming(string connectionId, CancellationToken cancellationToken);
}
