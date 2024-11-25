// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Services;

public class ScreenCastingService(IHubContext<ControlHub, IControlClient> hubContext, ILogger<ScreenCastingService> logger) : IScreenCastingService
{
    public void StartStreaming(IViewer viewer, int frameRate)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        viewer.FrameRate = frameRate;

        logger.LogInformation("Starting screen streaming for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);

        Task.Run(async () => await StreamScreenDataAsync(viewer, viewer.CancellationTokenSource.Token), viewer.CancellationTokenSource.Token);
    }

    public void StopStreaming(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        viewer.Dispose();
    }

    private async Task SendDisplays(IViewer viewer)
    {
        var displays = viewer.ScreenCapturing.GetDisplays();

        await hubContext.Clients.Client(viewer.ConnectionId).ReceiveDisplays(displays);
    }

    private static async IAsyncEnumerable<byte[]> StreamScreenDataAsync(IScreenCapturingService screenCapturing, IViewer viewer, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var delay = 1000 / viewer.FrameRate;
            var screenData = screenCapturing.GetNextFrame();

            if (screenData != null)
            {
                yield return screenData;
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task StreamScreenDataAsync(IViewer viewer, CancellationToken cancellationToken)
    {
        await SendDisplays(viewer);

        try
        {
            await foreach (var screenData in StreamScreenDataAsync(viewer.ScreenCapturing, viewer, cancellationToken))
            {
                await hubContext.Clients.Client(viewer.ConnectionId).ReceiveScreenUpdate(screenData);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Screen streaming was canceled for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred during streaming: {Message}", ex.Message);
        }
    }
}
