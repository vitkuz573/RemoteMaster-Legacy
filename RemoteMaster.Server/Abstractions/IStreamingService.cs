namespace RemoteMaster.Server.Abstractions;

public interface IStreamingService
{
    Task StartStreaming(string connectionId, CancellationToken cancellationToken);
}
