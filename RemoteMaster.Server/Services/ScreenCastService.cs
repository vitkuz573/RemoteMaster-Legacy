using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Dto;

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

                var screenDataChunks = SplitScreenData(screenData, 8192).ToList();

                for (int i = 0; i < screenDataChunks.Count; i++)
                {
                    var chunk = screenDataChunks[i];
                    var dto = new ScreenUpdateDto
                    {
                        Data = chunk,
                        IsEndOfImage = i == screenDataChunks.Count - 1
                    };

                    await _hubContext.Clients.Client(connectionId).SendAsync("ScreenUpdate", dto, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    private static IEnumerable<byte[]> SplitScreenData(byte[] screenData, int chunkSize)
    {
        for (var i = 0; i < screenData.Length; i += chunkSize)
        {
            yield return screenData.Skip(i).Take(chunkSize).ToArray();
        }
    }
}
