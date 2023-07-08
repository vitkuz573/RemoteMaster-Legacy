using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;

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

    public void SetFps(string controlId, int fps)
    {
        if (fps <= 0 || fps > 60)
        {
            _logger.LogError("FPS value should be between 1 and 60. Given: {fps}", fps);
            return;
        }

        var config = _screenCaptureService.GetClientConfig(controlId);
        config.FPS = fps;
    }

    public async Task StartStreaming(string controlId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screen stream for control ID {controlId}", controlId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCaptureService.CaptureScreen();
                await _hubContext.Clients.Client(controlId).SendAsync("ScreenUpdate", screenData, cancellationToken);

                var config = _screenCaptureService.GetClientConfig(controlId);
                await Task.Delay(1000 / config.FPS, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }
}
