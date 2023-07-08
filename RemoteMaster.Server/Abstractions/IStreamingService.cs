namespace RemoteMaster.Server.Abstractions;

public interface IStreamingService
{
    Task StartStreaming(string controlId, CancellationToken cancellationToken);

    void SetFps(string controlId, int fps);
}
