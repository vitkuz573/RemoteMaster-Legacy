using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private readonly IScreenCasterService _streamingService;
    private readonly IViewerService _viewerService;
    private readonly ILogger<ControlHub> _logger;
    private readonly IInputSender _inputSender;
    private CancellationTokenSource _cancellationTokenSource;

    public ControlHub(ILogger<ControlHub> logger, IScreenCasterService streamingService, IViewerService viewerService, IInputSender inputSender)
    {
        _logger = logger;
        _streamingService = streamingService;
        _viewerService = viewerService;
        _inputSender = inputSender;
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

    public void SendMouseCoordinates(int x, int y)
    {
        _logger.LogInformation($"Received mouse coordinates: ({x}, {y})");
        _inputSender.SendMouseCoordinates(x, y);
    }
}
