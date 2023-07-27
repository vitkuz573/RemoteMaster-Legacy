using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Native.Windows;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Hubs;

public class ControlHub : Hub
{
    private CancellationTokenSource _cancellationTokenSource;
    private readonly IScreenCaster _screenCaster;
    private readonly IInputSender _inputSender;
    private readonly IViewerStore _viewerStore;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(IScreenCaster screenCaster, IInputSender inputSender, IViewerStore viewerStore, ILogger<ControlHub> logger)
    {
        _screenCaster = screenCaster;
        _inputSender = inputSender;
        _viewerStore = viewerStore;
        _logger = logger;
    }

    public async override Task OnConnectedAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var connectionId = Context.ConnectionId;

        var _ = Task.Run(async () =>
        {
            try
            {
                await _screenCaster.StartStreaming(connectionId, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while streaming");
            }
        });
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        if (!_viewerStore.TryRemoveViewer(Context.ConnectionId))
        {
            _logger.LogError("Failed to remove viewer for connection ID {connectionId}", Context.ConnectionId);
        }

        _cancellationTokenSource?.Cancel();
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
        TokenPrivilegeHelper.AdjustTokenPrivilege(SE_SHUTDOWN_NAME);
        InitiateSystemShutdown(null, null, 0, true, true);
    }

    public async Task SendMessageBox(MessageBoxDto dto)
    {
        MessageBox(HWND.Null, dto.Text, dto.Caption, dto.Style);
    }

    private void ExecuteActionForViewer(Action<Viewer> action)
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
