using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;

namespace RemoteMaster.Server.Services;

public class Viewer
{
    private readonly IScreenCapturer _screenCapturer;
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<Viewer> _logger;
    private readonly string _connectionId;

    public Viewer(IScreenCapturer screenCapturer, ILogger<Viewer> logger, IHubContext<ControlHub> hubContext, string connectionId)
    {
        _screenCapturer = screenCapturer;
        _hubContext = hubContext;
        _logger = logger;
        _connectionId = connectionId;

        _screenCapturer.ScreenChanged += (sender, bounds) =>
        {
            // logic
        };
    }

    public async Task StartStreaming(CancellationToken cancellationToken)
    {
        var bounds = _screenCapturer.CurrentScreenBounds;

        await SendScreenData(_screenCapturer.GetDisplayNames(), _screenCapturer.SelectedScreen, bounds.Width, bounds.Height);

        _logger.LogInformation("Starting screen stream for ID {connectionId}", _connectionId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCapturer.GetNextFrame();

                var screenDataChunks = Chunker.ChunkifyBytes(screenData);

                foreach (var chunk in screenDataChunks)
                {
                    await _hubContext.Clients.Client(_connectionId).SendAsync("ScreenUpdate", chunk, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    public async Task SendScreenData(IEnumerable<string> displayNames, string selectedDisplay, int screenWidth, int screenHeight)
    {
        var dto = new ScreenDataDto
        {
            DisplayNames = displayNames,
            SelectedDisplay = selectedDisplay,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight
        };

        await SendDtoToViewer(dto);
    }

    public void SetSelectedScreen(SelectScreenDto dto)
    {
        _screenCapturer.SetSelectedScreen(dto.DisplayName);
    }

    private async Task SendDtoToViewer<T>(T dto) where T : class
    {
        await _hubContext.Clients.Client(_connectionId).SendAsync("ScreenData", dto);
    }
}
