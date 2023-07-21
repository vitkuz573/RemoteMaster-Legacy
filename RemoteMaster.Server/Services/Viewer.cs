using Microsoft.AspNetCore.SignalR;
using NAudio.Wave;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using System.Drawing;

namespace RemoteMaster.Server.Services;

public class Viewer
{
    private readonly IHubContext<ControlHub> _hubContext;
    private readonly ILogger<Viewer> _logger;

    public Viewer(IScreenCapturer screenCapturer, IAudioCapturer audioCapturer, ILogger<Viewer> logger, IHubContext<ControlHub> hubContext, string connectionId)
    {
        ScreenCapturer = screenCapturer;
        AudioCapturer = audioCapturer;
        _hubContext = hubContext;
        _logger = logger;
        ConnectionId = connectionId;

        ScreenCapturer.ScreenChanged += async (sender, bounds) =>
        {
            await SendScreenSize(bounds.Width, bounds.Height);
        };

        // AudioCapturer.DataAvailable += async (sender, e) =>
        // {
        //     var audioData = new byte[e.BytesRecorded];
        //     Array.Copy(e.Buffer, audioData, e.BytesRecorded);
        // 
        //     var audioDataChunks = Chunker.ChunkifyBytes(audioData);
        // 
        //     foreach (var chunk in audioDataChunks)
        //     {
        //         await _hubContext.Clients.Client(ConnectionId).SendAsync("AudioUpdate", chunk);
        //     }
        // };

        AudioCapturer.DataAvailable += async (sender, e) =>
        {
            var audioDataChunks = Chunker.ChunkifyBytes(e.Buffer);

            foreach (var chunk in audioDataChunks)
            {
                await _hubContext.Clients.Client(ConnectionId).SendAsync("AudioUpdate", chunk);
            }
        };
    }

    public IScreenCapturer ScreenCapturer { get; }

    public IAudioCapturer AudioCapturer { get; }

    public string ConnectionId { get; }

    public async Task StartStreaming(CancellationToken cancellationToken)
    {
        var bounds = ScreenCapturer.CurrentScreenBounds;

        await SendScreenData(ScreenCapturer.GetDisplays(), ScreenCapturer.SelectedScreen, bounds.Width, bounds.Height);

        _logger.LogInformation("Starting screen stream for ID {connectionId}", ConnectionId);

        AudioCapturer.StartCapturing();

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

        AudioCapturer.StopCapturing();
    }

    public async Task SendScreenData(IEnumerable<(string, bool, Size)> displays, string selectedDisplay, int screenWidth, int screenHeight)
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

    public void SetSelectedScreen(string displayName)
    {
        ScreenCapturer.SetSelectedScreen(displayName);
    }
}
