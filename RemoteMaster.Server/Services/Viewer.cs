using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Services;

public class Viewer
{
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<Viewer> _logger;

    public Viewer(IScreenCapturer screenCapturer, ILogger<Viewer> logger, IHubContext<ControlHub> hubContext, string connectionId)
    {
        ScreenCapturer = screenCapturer;
        _hubContext = hubContext;
        _logger = logger;
        ConnectionId = connectionId;

        ScreenCapturer.ScreenChanged += async (sender, bounds) => await SendScreenSize(bounds.Width, bounds.Height);
    }

    public IScreenCapturer ScreenCapturer { get; }

    public string ConnectionId { get; }

    public async Task StartStreaming(CancellationToken cancellationToken)
    {
        var bounds = ScreenCapturer.CurrentScreenBounds;

        await SendScreenData(ScreenCapturer.GetDisplays(), bounds.Width, bounds.Height);

        _logger.LogInformation("Starting screen stream for ID {connectionId}", ConnectionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = ScreenCapturer.GetNextFrame();

                var screenDataChunks = Chunker.ChunkifyBytes(screenData);

                foreach (var chunk in screenDataChunks)
                {
                    await _hubContext.Clients.Client(ConnectionId).SendAsync("ScreenUpdate", chunk, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    public async Task SendScreenData(IEnumerable<DisplayInfo> displays, int screenWidth, int screenHeight)
    {
        var dto = new ScreenDataDto
        {
            Displays = displays,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight
        };

        await _hubContext.Clients.Client(ConnectionId).SendAsync("ScreenData", dto);
    }

    public async Task SendScreenSize(int width, int height)
    {
        var dto = new ScreenSizeDto
        {
            Width = width,
            Height = height
        };

        await _hubContext.Clients.Client(ConnectionId).SendAsync("ScreenSize", dto);
    }

    public void SetSelectedScreen(string displayName)
    {
        ScreenCapturer.SetSelectedScreen(displayName);
    }
}
