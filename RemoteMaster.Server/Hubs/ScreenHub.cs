using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using System.Collections.Concurrent;
using System.Net;

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
        if (fps <= 0 || fps > 60)
        {
            _logger.LogError("FPS value should be between 1 and 60. Given: {fps}", fps);
            return;
        }

        if (!IPAddress.TryParse(ipAddress, out _))
        {
            _logger.LogError("Invalid IP address: {ipAddress}", ipAddress);
            return;
        }

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
                await Clients.OthersInGroup(ipAddress).SendAsync("ScreenUpdate", screenData, cancellationToken);

                var config = _screenCaptureService.GetClientConfig(ipAddress);
                await Task.Delay(1000 / config.FPS, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
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

        await StartScreenStream(ipAddress, cancellationTokenSource.Token);
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

