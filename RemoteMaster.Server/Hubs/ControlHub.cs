using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dto;

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

    public void SetQuality(int quality)
    {
        _viewerService.SetImageQuality(quality);
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        _inputSender.SendMouseCoordinates(dto);
    }

    public void SendMouseButton(MouseButtonClickDto dto)
    {
        _inputSender.SendMouseButton(dto);
    }

    public void SendMouseWheel(MouseWheelDto dto)
    {
        _inputSender.SendMouseWheel(dto);
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputSender.SendKeyboardInput(dto);
    }
}
