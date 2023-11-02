// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class Viewer : IViewer
{
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private CancellationTokenSource _streamingCts;

    public Viewer(IScreenCapturerService screenCapturer, IHubContext<ControlHub, IControlClient> hubContext, string connectionId)
    {
        ScreenCapturer = screenCapturer;
        _hubContext = hubContext;
        ConnectionId = connectionId;

        _ = SendHostVersion();

        ScreenCapturer.ScreenChanged += async (sender, bounds) => await SendScreenSize(bounds.Width, bounds.Height);
    }

    public IScreenCapturerService ScreenCapturer { get; }

    public string ConnectionId { get; }

    public async Task StartStreaming()
    {
        _streamingCts = new CancellationTokenSource();
        var cancellationToken = _streamingCts.Token;

        await SendScreenData(ScreenCapturer.GetDisplays());

        Log.Information("Starting screen stream for ID {connectionId}", ConnectionId);

        try
        {
            await foreach (var screenData in StreamScreenDataAsync(cancellationToken))
            {
                await _hubContext.Clients.Client(ConnectionId).ReceiveScreenUpdate(screenData);
            }
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred during streaming: {Message}", ex.Message);
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
        Log.Information("Stopping screen stream for ID {connectionId}", ConnectionId);

        _streamingCts?.Cancel();
    }

    public async Task SendScreenData(IEnumerable<DisplayInfo> displays)
    {
        var dto = new ScreenDataDto
        {
            Displays = displays
        };

        await _hubContext.Clients.Client(ConnectionId).ReceiveScreenData(dto);
    }

    public async Task SendScreenSize(int width, int height)
    {
        await _hubContext.Clients.Client(ConnectionId).ReceiveScreenSize(new Size(width, height));
    }

    public async Task SendHostVersion()
    {
        await _hubContext.Clients.Clients(ConnectionId).ReceiveHostVersion(Assembly.GetExecutingAssembly().GetName().Version ?? new Version());
    }

    public void SetSelectedScreen(string displayName)
    {
        ScreenCapturer.SetSelectedScreen(displayName);
    }
}
