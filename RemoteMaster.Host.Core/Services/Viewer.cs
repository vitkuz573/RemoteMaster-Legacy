// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
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
    private bool _disposed;

    public Viewer(IHubContext<ControlHub, IControlClient> hubContext, IScreenCapturerService screenCapturer, string connectionId, string userName)
    {
        _hubContext = hubContext;

        ScreenCapturer = screenCapturer;
        ConnectionId = connectionId;
        UserName = userName;
        ConnectedTime = DateTime.UtcNow;

        _cts = new();

        ScreenCapturer.ScreenChanged += async (_, bounds) => await SendScreenSize(bounds.Width, bounds.Height);

        _ = StartStreaming();
    }

    public IScreenCapturerService ScreenCapturer { get; }

    public string ConnectionId { get; }

    public string UserName { get; }

    public DateTime ConnectedTime { get; }

    private async Task StartStreaming()
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

            await Task.Delay(16, cancellationToken);
        }
    }

    public void StopStreaming()
    {
        Log.Information("Stopping screen stream for ID {connectionId}", ConnectionId);

        _cts.Cancel();
    }

    private async Task SendDisplays(IEnumerable<Display> displays)
    {
        await _hubContext.Clients.Client(ConnectionId).ReceiveDisplays(displays);
    }

    private async Task SendScreenSize(int width, int height)
    {
        await _hubContext.Clients.Client(ConnectionId).ReceiveScreenSize(new Size(width, height));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _disposed = true;
    }

    ~Viewer()
    {
        Dispose(false);
    }
}
