using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Client.Models;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dto;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly ConcurrentDictionary<string, Viewer> _connectedViewers = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellations = new();
    private readonly IStreamingService _streamingService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(ILogger<ControlHub> logger, IStreamingService streamingService)
    {
        _logger = logger;
        _streamingService = streamingService;
    }

    public async Task SendCursorPosition(string controlId, CursorPositionDto cursorPosition)
    {
        _logger.LogInformation("SendCursorPosition called with control ID: {controlId} and cursor position: {cursorPosition}", controlId, cursorPosition);
        _logger.LogInformation($"Received cursor position: ({cursorPosition.X}, {cursorPosition.Y}) from control ID: {controlId}");
        await Clients.Group(controlId).SendAsync("ReceiveCursorPosition", cursorPosition);
        _logger.LogInformation($"Sent cursor position to the clients in group {controlId}");
    }

    public async Task SetFps(string controlId, int fps)
    {
        _streamingService.SetFps(controlId, fps);
        await Clients.Group(controlId).SendAsync("FpsUpdated", fps);
    }

    public override async Task OnConnectedAsync()
    {
        var controlId = Context.GetHttpContext()?.Request.Headers["controlId"].ToString();

        if (string.IsNullOrEmpty(controlId))
        {
            _logger.LogWarning("Control ID not found in the request headers.");
        }
        else
        {
            _logger.LogInformation($"Client with control ID: {controlId} connected. Adding to group...");
            await Groups.AddToGroupAsync(Context.ConnectionId, controlId);
            _logger.LogInformation($"Client with control ID: {controlId} added to group.");

            var viewer = new Viewer
            {
                ConnectionId = Context.ConnectionId,
                IpAddress = Context.GetHttpContext()?.Connection.RemoteIpAddress.ToString(),
                ConnectedAt = DateTime.UtcNow
            };

            _connectedViewers[controlId] = viewer;

            var cancellationTokenSource = new CancellationTokenSource();
            _connectionCancellations[controlId] = cancellationTokenSource;
            await _streamingService.StartStreaming(controlId, cancellationTokenSource.Token);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var controlId = Context.GetHttpContext()?.Request.Headers["controlId"].ToString();

        if (string.IsNullOrEmpty(controlId))
        {
            _logger.LogWarning("Control ID not found in the request headers.");
        }
        else
        {
            _logger.LogInformation($"Client with control ID: {controlId} disconnected. Removing from group...");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, controlId);
            _logger.LogInformation($"Client with control ID: {controlId} removed from group.");

            _connectedViewers.TryRemove(controlId, out var _);

            if (_connectionCancellations.TryRemove(controlId, out var cancellationTokenSource))
            {
                cancellationTokenSource.Cancel();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
