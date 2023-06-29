using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dto;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellations = new();
    private readonly IStreamingService _streamingService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(ILogger<ControlHub> logger, IStreamingService streamingService)
    {
        _logger = logger;
        _streamingService = streamingService;
    }

    public void SetFps(string screenId, int fps)
    {
        _streamingService.SetFps(screenId, fps);
    }

    public async Task SendMouseMovement(string screenId, MouseMovementDto mouseMovement)
    {
        await Clients.Group(screenId).SendAsync("ReceiveMouseMovement", mouseMovement);
    }

    public async Task SendMouseButtonClick(string screenId, MouseButtonClickDto mouseButtonClick)
    {
        await Clients.Group(screenId).SendAsync("ReceiveMouseButtonClick", mouseButtonClick);
    }

    public override async Task OnConnectedAsync()
    {
        var screenId = Context.GetHttpContext()?.Request.Query["screenId"].ToString();

        if (!string.IsNullOrEmpty(screenId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, screenId);
            var cancellationTokenSource = new CancellationTokenSource();
            _connectionCancellations[screenId] = cancellationTokenSource;
            await _streamingService.StartStreaming(screenId, cancellationTokenSource.Token);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var screenId = Context.GetHttpContext()?.Request.Query["screenId"].ToString();

        if (!string.IsNullOrEmpty(screenId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, screenId);
            if (_connectionCancellations.TryRemove(screenId, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
