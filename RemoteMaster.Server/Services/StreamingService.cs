using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using System.Net;

namespace RemoteMaster.Server.Services;

public class StreamingService : IStreamingService
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly ILogger<StreamingService> _logger;
    private readonly IHubContext<ControlHub> _hubContext;

    public StreamingService(IScreenCaptureService screenCaptureService, ILogger<StreamingService> logger, IHubContext<ControlHub> hubContext)
    {
        _screenCaptureService = screenCaptureService;
        _logger = logger;
        _hubContext = hubContext;
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

    public async Task StartStreaming(string ipAddress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screen stream for IP {ipAddress}", ipAddress);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCaptureService.CaptureScreen();
                await _hubContext.Clients.Group(ipAddress).SendAsync("ScreenUpdate", screenData, cancellationToken);

                var config = _screenCaptureService.GetClientConfig(ipAddress);
                await Task.Delay(1000 / config.FPS, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }
}
