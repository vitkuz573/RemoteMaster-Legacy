using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;

namespace RemoteMaster.Server.Hubs;

public class ScreenHub : Hub
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellations = new();
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly ILogger<ScreenHub> _logger;

    public ScreenHub(ILogger<ScreenHub> logger, IScreenCaptureService screenCaptureService)
    {
        _logger = logger;
        _screenCaptureService = screenCaptureService;
    }

    public void SetFps(string ipAddress, int fps)
    {
        var config = _screenCaptureService.GetClientConfig(ipAddress);
        config.FPS = fps;
    }

    public async Task StartScreenStream(string ipAddress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screen stream for IP {ipAddress}", ipAddress);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCaptureService.CaptureScreen();
                await Clients.OthersInGroup(ipAddress).SendAsync("ScreenUpdate", screenData);

                var config = _screenCaptureService.GetClientConfig(ipAddress);
                await Task.Delay(1000 / config.FPS);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for the connection.");
            return;
        }

        var ipAddress = httpContext.Request.Query["ipAddress"];

        await Groups.AddToGroupAsync(Context.ConnectionId, ipAddress);

        var cancellationTokenSource = new CancellationTokenSource();
        _connectionCancellations[ipAddress] = cancellationTokenSource;

        await StartScreenStream(ipAddress, cancellationTokenSource.Token);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for the connection.");
            return;
        }

        var ipAddress = httpContext.Request.Query["ipAddress"];

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ipAddress);

        if (_connectionCancellations.TryRemove(ipAddress, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }

        await base.OnDisconnectedAsync(exception);
    }
}

