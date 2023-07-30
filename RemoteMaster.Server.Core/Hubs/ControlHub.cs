using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Core.Hubs;

public class ControlHub : Hub
{
    private readonly IScreenCaster _screenCaster;
    private readonly IInputSender _inputSender;
    private readonly IViewerStore _viewerStore;
    private readonly IPowerManager _powerManager;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(IScreenCaster screenCaster, IInputSender inputSender, IViewerStore viewerStore, IPowerManager powerManager, ILogger<ControlHub> logger)
    {
        _screenCaster = screenCaster;
        _inputSender = inputSender;
        _viewerStore = viewerStore;
        _powerManager = powerManager;
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;

        var _ = Task.Run(async () =>
        {
            try
            {
                await _screenCaster.StartStreaming(connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while streaming");
            }
        });

        return Task.CompletedTask;
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        _screenCaster.StopStreaming(connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        ExecuteActionForViewer(viewer => _inputSender.SendMouseCoordinates(dto, viewer));
    }

    public void SendMouseButton(MouseClickDto dto)
    {
        ExecuteActionForViewer(viewer => _inputSender.SendMouseButton(dto, viewer));
    }

    public void SendMouseWheel(MouseWheelDto dto)
    {
        _inputSender.SendMouseWheel(dto);
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputSender.SendKeyboardInput(dto);
    }

    public void SendSelectedScreen(string displayName)
    {
        _screenCaster.SetSelectedScreen(Context.ConnectionId, displayName);
    }

    public void SetInputEnabled(bool inputEnabled)
    {
        _inputSender.InputEnabled = inputEnabled;
    }

    public void SetQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.Quality = quality);
    }

    public void SetTrackCursor(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    public async Task KillServer()
    {
        Environment.Exit(0);
    }

    public async Task RebootComputer()
    {
        _powerManager.Reboot();
    }

    private void ExecuteActionForViewer(Action<IViewer> action)
    {
        if (_viewerStore.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            action(viewer);
        }
        else
        {
            _logger.LogError("Failed to find a viewer for connection ID {connectionId}", Context.ConnectionId);
        }
    }
}
