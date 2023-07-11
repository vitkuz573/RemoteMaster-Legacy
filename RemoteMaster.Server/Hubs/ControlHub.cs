using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly IScreenCasterService _streamingService;
    private readonly IViewerService _viewerService;
    private readonly ILogger<ControlHub> _logger;
    private CancellationTokenSource _cancellationTokenSource;

    public ControlHub(ILogger<ControlHub> logger, IScreenCasterService streamingService, IViewerService viewerService)
    {
        _logger = logger;
        _streamingService = streamingService;
        _viewerService = viewerService;
    }

    public override async Task OnConnectedAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        var connectionId = Context.ConnectionId;

        var _ = Task.Run(async () =>
        {
            try
            {
                await _streamingService.StartStreaming(connectionId, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while streaming");
            }
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _cancellationTokenSource.Cancel();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SetQuality(int quality)
    {
        _logger.LogInformation("Invoked SetQuality");

        _viewerService.SetImageQuality(quality);
    }

    public async Task SendMouseCoordinates(int x, int y, double imgWidth, double imgHeight)
    {
        _logger.LogInformation($"Received mouse coordinates: ({x}, {y}) and image dimensions: ({imgWidth}, {imgHeight})");

        // переводим координаты мыши в абсолютные координаты для SendInput
        var translatedX = (int)(x * 65535 / imgWidth);
        var translatedY = (int)(y * 65535 / imgHeight);

        // выводим полученные и переведенные координаты в лог
        _logger.LogInformation($"Translated mouse coordinates: ({translatedX}, {translatedY})");

        // здесь вы можете обработать переведенные координаты мыши как вам нужно
        // например, можно вызывать Win32 API SendInput:
        // Win32ApiHelper.SendMouseInput(translatedX, translatedY);
    }
}
