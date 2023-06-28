using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;
using System.Net;

namespace RemoteMaster.Server.Hubs;

public class ScreenHub : Hub
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellations = new();
    private readonly IStreamingService _streamingService;
    private readonly ILogger<ScreenHub> _logger;

    public ScreenHub(ILogger<ScreenHub> logger, IStreamingService streamingService)
    {
        _logger = logger;
        _streamingService = streamingService;
    }

    public void SetFps(string ipAddress, int fps)
    {
        _streamingService.SetFps(ipAddress, fps);
    }

    public override async Task OnConnectedAsync()
    {
        var ipAddress = GetIpAddressFromHttpContext();

        if (ipAddress == null)
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ipAddress);

        var cancellationTokenSource = new CancellationTokenSource();
        _connectionCancellations[ipAddress] = cancellationTokenSource;

        await _streamingService.StartStreaming(ipAddress, cancellationTokenSource.Token);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var ipAddress = GetIpAddressFromHttpContext();

        if (ipAddress == null)
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ipAddress);

        if (_connectionCancellations.TryRemove(ipAddress, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }

        await base.OnDisconnectedAsync(exception);
    }

    private string? GetIpAddressFromHttpContext()
    {
        var ipAddress = Context.GetHttpContext()?.Request.Query["ipAddress"].ToString();

        if (string.IsNullOrEmpty(ipAddress) || !IPAddress.TryParse(ipAddress, out _))
        {
            _logger.LogWarning("Invalid or missing IP address.");
            return null;
        }

        return ipAddress;
    }
}
