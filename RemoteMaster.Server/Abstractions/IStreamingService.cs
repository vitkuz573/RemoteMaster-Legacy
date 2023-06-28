using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Server.Abstractions;

public interface IStreamingService
{
    Task StartStreaming(string ipAddress, CancellationToken cancellationToken);

    void SetFps(string ipAddress, int fps);
}
