using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;

namespace RemoteMaster.Server.Services;

public class StreamingService : IStreamingService
{
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly ILogger<StreamingService> _logger;
    private readonly IHubContext<ControlHub> _hubContext;

    private readonly byte[] _endOfImageMarker = new byte[] { 255, 255, 255, 255 };

    public StreamingService(IScreenCaptureService screenCaptureService, ILogger<StreamingService> logger, IHubContext<ControlHub> hubContext)
    {
        _screenCaptureService = screenCaptureService;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task StartStreaming(string connectionId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screen stream for ID {connectionId}", connectionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCaptureService.CaptureScreen();

                var screenDataChunks = SplitScreenData(screenData);

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

    private IEnumerable<byte[]> SplitScreenData(byte[] screenData)
    {
        var bufferSize = 8192;

        for (var i = 0; i < screenData.Length; i += bufferSize)
        {
            yield return screenData.Skip(i).Take(bufferSize).ToArray();
        }

        yield return _endOfImageMarker;
    }
}
