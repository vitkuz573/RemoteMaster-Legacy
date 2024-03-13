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
    private readonly IAppState _appState;
    private readonly IHubContext<ControlHub, IControlClient> _hubContext;
    private readonly CancellationTokenSource _cts;
    private bool _disposed;

    public Viewer(IAppState appState, IHubContext<ControlHub, IControlClient> hubContext, IScreenCapturerService screenCapturer, string connectionId)
    {
        ArgumentNullException.ThrowIfNull(appState);

        _appState = appState;
        _hubContext = hubContext;

        ScreenCapturer = screenCapturer;
        ConnectionId = connectionId;

        _appState.ViewerAdded += AppState_ViewerAdded;
        _appState.ViewerRemoved += AppState_ViewerRemoved;

        _cts = new();

        _ = SendHostVersion();

        ScreenCapturer.ScreenChanged += async (_, bounds) => await SendScreenSize(bounds.Width, bounds.Height);
    }

    private async void AppState_ViewerAdded(object? sender, IViewer e)
    {
        if (e.ConnectionId == ConnectionId)
        {
            await StartStreaming();
        }
    }

    private void AppState_ViewerRemoved(object? sender, IViewer e)
    {
        if (e.ConnectionId == ConnectionId)
        {
            StopStreaming();
        }
    }

    public IScreenCapturerService ScreenCapturer { get; }

    public string ConnectionId { get; }

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

    private void StopStreaming()
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

    private async Task SendHostVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version ?? new Version();

        await _hubContext.Clients.Clients(ConnectionId).ReceiveHostVersion(version);
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

            _appState.ViewerAdded -= AppState_ViewerAdded;
            _appState.ViewerRemoved -= AppState_ViewerRemoved;
        }

        _disposed = true;
    }

    ~Viewer()
    {
        Dispose(false);
    }
}
