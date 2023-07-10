using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly IScreenCasterService _streamingService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(ILogger<ControlHub> logger, IScreenCasterService streamingService)
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
