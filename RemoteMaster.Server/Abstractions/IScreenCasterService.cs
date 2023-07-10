namespace RemoteMaster.Server.Abstractions;

public interface IScreenCasterService
{
    Task StartStreaming(string connectionId, CancellationToken cancellationToken);
}
