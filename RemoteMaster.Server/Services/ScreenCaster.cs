using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;

namespace RemoteMaster.Server.Services;

public class ScreenCaster : IScreenCaster
{
    private readonly IScreenCapturer _screenCapturer;
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<ScreenCaster> _logger;

    public ScreenCaster(IScreenCapturer screenCapturer, ILogger<ScreenCaster> logger, IHubContext<ControlHub> hubContext)
    {
        _screenCapturer = screenCapturer;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task StartStreaming(string connectionId, CancellationToken cancellationToken)
    {
        _screenCapturer.ScreenChanged += (sender, bounds) =>
        {
            // logic
        };

        _logger.LogInformation("Starting screen stream for ID {connectionId}", connectionId);

        await _hubContext.Clients.Client(connectionId).SendAsync("Displays", _screenCapturer.GetDisplayNames().ToArray(), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var screenData = _screenCapturer.GetNextFrame();

                var screenDataChunks = Chunker.ChunkifyBytes(screenData);

                foreach (var chunk in screenDataChunks)
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ScreenUpdate", chunk, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    public void SetSelectedScreen(SelectScreenDto dto)
    {
        _screenCapturer.SetSelectedScreen(dto.DisplayName);
    }
}
