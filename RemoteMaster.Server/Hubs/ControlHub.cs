using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Native.Windows;
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

    public override async Task OnConnectedAsync()
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _cancellationTokenSource.Cancel();
        await base.OnDisconnectedAsync(exception);
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        var viewer = _viewerStore.GetViewer(Context.ConnectionId);

        _inputSender.SendMouseCoordinates(dto, viewer);
    }

    public void SendMouseButton(MouseButtonClickDto dto)
    {
        var viewer = _viewerStore.GetViewer(Context.ConnectionId);

        _inputSender.SendMouseButton(dto, viewer);
    }

    public void SendMouseWheel(MouseWheelDto dto)
    {
        _inputSender.SendMouseWheel(dto);
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputSender.SendKeyboardInput(dto);
    }

    public void SendSelectedScreen(SelectScreenDto dto)
    {
        _screenCaster.SetSelectedScreen(Context.ConnectionId, dto);
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
}
