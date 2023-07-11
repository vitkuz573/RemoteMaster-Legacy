using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using System;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Input.KeyboardAndMouse;
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

    public void SendMouseCoordinates(int x, int y, double imgWidth, double imgHeight)
    {
        _logger.LogInformation($"Received mouse coordinates: ({x}, {y}) and image dimensions: ({imgWidth}, {imgHeight})");

        // переводим координаты мыши в абсолютные координаты для SendInput
        var translatedX = (int)(x * 65535 / imgWidth);
        var translatedY = (int)(y * 65535 / imgHeight);

        var input = new INPUT
        {
            type = INPUT_TYPE.INPUT_MOUSE
        };

        input.Anonymous.mi = new MOUSEINPUT
        {
            dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
            dx = translatedX,
            dy = translatedY,
            time = 0,
            mouseData = 0,
            dwExtraInfo = (nuint)GetMessageExtraInfo().Value
        };

        var inputs = new Span<INPUT>(ref input);

        SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}
