using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Helpers;

namespace RemoteMaster.Server.Services;

public class ScreenCastService : IScreenCasterService
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<ScreenCastService> _logger;

    public ScreenCastService(IScreenCaptureService screenCaptureService, ILogger<ScreenCastService> logger, IHubContext<ControlHub> hubContext)
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

                var screenDataChunks = Chunker.Chunkify(screenData);

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
