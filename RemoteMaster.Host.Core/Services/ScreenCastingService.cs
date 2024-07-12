// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;
using Serilog;

namespace RemoteMaster.Host.Core.Services;

public class ScreenCastingService(IHubContext<ControlHub, IControlClient> hubContext) : IScreenCastingService
{
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _streamingTasks = new();

    public void StartStreaming(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        var cancellationTokenSource = new CancellationTokenSource();
        _streamingTasks[viewer.ConnectionId] = cancellationTokenSource;

        viewer.ScreenCapturer.ScreenChanged += async (_, bounds) => await SendScreenSize(viewer, bounds.Width, bounds.Height);

        Task.Run(async () => await StreamScreenDataAsync(viewer, cancellationTokenSource.Token), cancellationTokenSource.Token);
    }

    public void StopStreaming(string connectionId)
    {
        if (!_streamingTasks.TryRemove(connectionId, out var cancellationTokenSource))
        {
            return;
        }

        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    private async Task SendDisplays(IViewer viewer)
    {
        var displays = viewer.ScreenCapturer.GetDisplays();
        await hubContext.Clients.Client(viewer.ConnectionId).ReceiveDisplays(displays);
    }

    private async Task SendScreenSize(IViewer viewer, int width, int height)
    {
        await hubContext.Clients.Client(viewer.ConnectionId).ReceiveScreenSize(new Size(width, height));
    }

    private async Task StreamScreenDataAsync(IViewer viewer, CancellationToken cancellationToken)
    {
        await SendDisplays(viewer);

        Log.Information("Starting screen stream for ID {connectionId}", viewer.ConnectionId);

        try
        {
            await foreach (var screenData in StreamScreenDataAsync(viewer.ScreenCapturer, cancellationToken))
            {
                await hubContext.Clients.Client(viewer.ConnectionId).ReceiveScreenUpdate(screenData);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("Screen streaming was canceled for ID {connectionId}", viewer.ConnectionId);
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred during streaming: {Message}", ex.Message);
        }
    }

    private static async IAsyncEnumerable<byte[]> StreamScreenDataAsync(IScreenCapturerService screenCapturer, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var screenData = screenCapturer.GetNextFrame();

            if (screenData != null)
            {
                yield return screenData;
            }

            await Task.Delay(16, cancellationToken);
        }
    }
}
