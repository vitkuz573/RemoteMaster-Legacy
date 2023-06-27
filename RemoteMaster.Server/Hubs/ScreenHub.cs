using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Hubs;

public class ScreenHub : Hub
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellations = new();
    private readonly ConcurrentDictionary<string, int> _fpsSettings = new();
    private readonly ILogger<ScreenHub> _logger;

    public ScreenHub(ILogger<ScreenHub> logger)
    {
        _logger = logger;
    }

    public void SetFps(string ipAddress, int fps)
    {
        if (fps <= 0)
        {
            _logger.LogError("FPS value should be greater than 0. Given: {fps}", fps);
            return;
        }

        _fpsSettings[ipAddress] = fps;
    }

    public byte[] CaptureScreen()
    {
        var width = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN);

        using var bitmap = new Bitmap(width, height);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        }

        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);

        return memoryStream.ToArray();
    }

    public async Task StartScreenStream(string ipAddress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting screen stream for IP {ipAddress}", ipAddress);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = CaptureScreen();
                await Clients.OthersInGroup(ipAddress).SendAsync("ScreenUpdate", screenData);

                var fps = _fpsSettings.TryGetValue(ipAddress, out var val) ? val : 30;

                await Task.Delay(1000 / fps);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for the connection.");
            return;
        }

        var ipAddress = httpContext.Request.Query["ipAddress"];

        await Groups.AddToGroupAsync(Context.ConnectionId, ipAddress);

        var cancellationTokenSource = new CancellationTokenSource();
        _connectionCancellations[ipAddress] = cancellationTokenSource;

        // Initialize FPS settings with a default value
        _fpsSettings[ipAddress] = 30;

        await StartScreenStream(ipAddress, cancellationTokenSource.Token);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
            _logger.LogWarning("No HTTP context available for the connection.");
            return;
        }

        var ipAddress = httpContext.Request.Query["ipAddress"];

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ipAddress);

        if (_connectionCancellations.TryRemove(ipAddress, out var cancellationTokenSource))
        {
            cancellationTokenSource.Cancel();
        }

        await base.OnDisconnectedAsync(exception);
    }
}
