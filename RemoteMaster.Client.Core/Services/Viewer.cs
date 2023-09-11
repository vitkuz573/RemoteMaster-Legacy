// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Core.Services;

public class Viewer : IViewer
{
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly IConfigurationProvider _configurationProvider;
    private readonly ILogger<Viewer> _logger;
    private CancellationTokenSource _streamingCts;

    public Viewer(IScreenCapturerService screenCapturer, IConfigurationProvider configurationService, ILogger<Viewer> logger, IHubContext<ControlHub, IControlClient> hubContext, string connectionId)
    {
        ScreenCapturer = screenCapturer;
        _hubContext = hubContext;
        _configurationProvider = configurationService;
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

        await SendClientConfiguration();

        var bounds = ScreenCapturer.CurrentScreenBounds;

        await SendScreenData(ScreenCapturer.GetDisplays(), bounds.Width, bounds.Height);

        _logger.LogInformation("Starting screen stream for ID {connectionId}", ConnectionId);

        try
        {
            await foreach (var screenData in StreamScreenDataAsync(cancellationToken))
            {
                await _hubContext.Clients.Client(ConnectionId).ReceiveScreenUpdate(screenData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred during streaming: {Message}", ex.Message);
        }
    }

    private async IAsyncEnumerable<byte[]> StreamScreenDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var screenData = ScreenCapturer.GetNextFrame();

            if (screenData != null)
            {
                yield return screenData;
            }

            await Task.Delay(16); // Добавить задержку для управления частотой кадров
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

    private async Task SendClientConfiguration()
    {
        var configuration = _configurationProvider.Fetch();

        await _hubContext.Clients.Client(ConnectionId).ReceiveClientConfiguration(configuration);
    }

    public void SetSelectedScreen(string displayName)
    {
        ScreenCapturer.SetSelectedScreen(displayName);
    }
}
