using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly IStreamingService _streamingService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(ILogger<ControlHub> logger, IStreamingService streamingService)
    {
        _logger = logger;
        _streamingService = streamingService;
    }

    public override async Task OnConnectedAsync()
    {
        var cancellationTokenSource = new CancellationTokenSource();

        await _streamingService.StartStreaming(Context.ConnectionId, cancellationTokenSource.Token);
    }
}
