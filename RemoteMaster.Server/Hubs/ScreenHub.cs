using Microsoft.AspNetCore.SignalR;
using ScreenHelper;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;

namespace RemoteMaster.Server.Hubs;

public class ScreenHub : Hub
{
    private ConcurrentDictionary<string, CancellationTokenSource> _connectionCancellations = new ConcurrentDictionary<string, CancellationTokenSource>();
    private readonly ILogger<ScreenHub> _logger;

    public ScreenHub(ILogger<ScreenHub> logger)
    {
        _logger = logger;
    }

    public byte[] CaptureScreen()
    {
        using var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

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
            var screenData = CaptureScreen();
            await Clients.OthersInGroup(ipAddress).SendAsync("ScreenUpdate", screenData);

            await Task.Delay(1000 / 30);
        }
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
            return;
        }

        var ipAddress = httpContext.Request.Query["ipAddress"];

        await Groups.AddToGroupAsync(Context.ConnectionId, ipAddress);

        var cancellationTokenSource = new CancellationTokenSource();
        _connectionCancellations[ipAddress] = cancellationTokenSource;

        await StartScreenStream(ipAddress, cancellationTokenSource.Token);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var httpContext = Context.GetHttpContext();

        if (httpContext == null)
        {
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