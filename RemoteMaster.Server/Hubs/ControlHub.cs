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

    public void SetFps(string controlId, int fps)
    {
        _streamingService.SetFps(controlId, fps);
    }

    public async Task SendMouseMovement(string controlId, MouseMovementDto mouseMovement)
    {
        await Clients.Group(controlId).SendAsync("ReceiveMouseMovement", mouseMovement);
    }

    public async Task SendMouseButtonClick(string controlId, MouseButtonClickDto mouseButtonClick)
    {
        await Clients.Group(controlId).SendAsync("ReceiveMouseButtonClick", mouseButtonClick);
    }

    public override async Task OnConnectedAsync()
    {
        var controlId = Context.GetHttpContext()?.Request.Headers["controlId"].ToString();

        if (!string.IsNullOrEmpty(controlId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, controlId);
            var cancellationTokenSource = new CancellationTokenSource();
            _connectionCancellations[controlId] = cancellationTokenSource;
            await _streamingService.StartStreaming(controlId, cancellationTokenSource.Token);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var controlId = Context.GetHttpContext()?.Request.Headers["controlId"].ToString();

        if (!string.IsNullOrEmpty(controlId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, controlId);

            if (_connectionCancellations.TryRemove(controlId, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
