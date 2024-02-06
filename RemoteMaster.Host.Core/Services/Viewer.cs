// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class Viewer : IViewer
{
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly CancellationTokenSource _cts;

    public Viewer(IScreenCapturerService screenCapturer, IHubContext<ControlHub, IControlClient> hubContext, string connectionId)
    {
        ScreenCapturer = screenCapturer;
        _hubContext = hubContext;
        ConnectionId = connectionId;

        _cts = new();

        _ = SendHostVersion();

        ScreenCapturer.ScreenChanged += async (sender, bounds) => await SendScreenSize(bounds.Width, bounds.Height);
    }

    public IScreenCapturerService ScreenCapturer { get; }

    public string ConnectionId { get; }

    public async Task StartStreaming()
    {
        var cancellationToken = _cts.Token;

        await SendDisplays(ScreenCapturer.GetDisplays());

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

    private async IAsyncEnumerable<byte[]> StreamScreenDataAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var screenData = ScreenCapturer.GetNextFrame();

            if (screenData != null)
            {
                yield return screenData;
            }

            await Task.Delay(16);
        }
    }

    public void StopStreaming()
    {
        Log.Information("Stopping screen stream for ID {connectionId}", ConnectionId);

        _cts?.Cancel();
    }

    public async Task SendDisplays(IEnumerable<Display> displays)
    {
        await _hubContext.Clients.Client(ConnectionId).ReceiveDisplays(displays);
    }

    public async Task SendScreenSize(int width, int height)
    {
        await _hubContext.Clients.Client(ConnectionId).ReceiveScreenSize(new Size(width, height));
    }

    public async Task SendHostVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

        await _hubContext.Clients.Clients(ConnectionId).ReceiveHostVersion(version);
    }

    public void SetSelectedScreen(string displayName)
    {
        ScreenCapturer.SetSelectedScreen(displayName);
    }
}
