using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;

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

        ScreenCapturer.ScreenChanged += async (sender, bounds) =>
        {
            await SendScreenSize(bounds.Width, bounds.Height);
        };
    }

    public IScreenCapturer ScreenCapturer { get; }

    public string ConnectionId { get; }

    public async Task StartStreaming(CancellationToken cancellationToken)
    {
        var bounds = ScreenCapturer.CurrentScreenBounds;

        await SendScreenData(ScreenCapturer.GetDisplays(), ScreenCapturer.SelectedScreen, bounds.Width, bounds.Height);

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

    public async Task SendScreenData(IEnumerable<(string, bool)> displays, string selectedDisplay, int screenWidth, int screenHeight)
    {
        var dto = new ScreenDataDto
        {
            Displays = displays,
            SelectedDisplay = selectedDisplay,
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

    public void SetSelectedScreen(SelectScreenDto dto)
    {
        ScreenCapturer.SetSelectedScreen(dto.DisplayName);
    }
}
