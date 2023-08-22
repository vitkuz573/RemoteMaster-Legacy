// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Server.Core.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Core.Services;

public class Viewer : IViewer
{
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly ILogger<Viewer> _logger;
    private CancellationTokenSource _streamingCts;

    public Viewer(IScreenCapturerService screenCapturer, ILogger<Viewer> logger, IHubContext<ControlHub, IControlClient> hubContext, string connectionId)
    {
        ScreenCapturer = screenCapturer;
        _hubContext = hubContext;
        _logger = logger;
        ConnectionId = connectionId;

        ScreenCapturer.ScreenChanged += async (sender, bounds) => await SendScreenSize(bounds.Width, bounds.Height);
    }

    public IScreenCapturerService ScreenCapturer { get; }

    public string ConnectionId { get; }

    public async Task StartStreaming()
    {
        _streamingCts = new CancellationTokenSource();
        var cancellationToken = _streamingCts.Token;

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
                    await _hubContext.Clients.Client(ConnectionId).ReceiveScreenUpdate(chunk);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
            }
        }
    }

    public void StopStreaming()
    {
        _logger.LogInformation("Stopping screen stream for ID {connectionId}", ConnectionId);

        _streamingCts?.Cancel();
    }

    public async Task SendScreenData(IEnumerable<DisplayInfo> displays, int screenWidth, int screenHeight)
    {
        var dto = new ScreenDataDto
        {
            Displays = displays,
            ScreenSize = new Size(screenWidth, screenHeight)
        };

        await _hubContext.Clients.Client(ConnectionId).ReceiveScreenData(dto);
    }

    public async Task SendScreenSize(int width, int height)
    {
        await _hubContext.Clients.Client(ConnectionId).ReceiveScreenSize(new Size(width, height));
    }

    public void SetSelectedScreen(string displayName)
    {
        ScreenCapturer.SetSelectedScreen(displayName);
    }
}
