using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Helpers;

namespace RemoteMaster.Server.Services;

public class ScreenCaster : IScreenCaster
{
    private readonly IScreenCapturer _screenCaptureService;
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<ScreenCaster> _logger;

    public ScreenCaster(IScreenCapturer screenCaptureService, ILogger<ScreenCaster> logger, IHubContext<ControlHub> hubContext)
    {
        _screenCaptureService = screenCaptureService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task StartStreaming(string connectionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screen stream for ID {connectionId}", connectionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCaptureService.CaptureScreen();

                var screenDataChunks = Chunker.ChunkifyBytes(screenData);

                foreach (var chunk in screenDataChunks)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ScreenUpdate", chunk, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }
}
