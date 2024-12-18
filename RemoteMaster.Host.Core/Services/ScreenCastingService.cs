// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Services;

public class ScreenCastingService(IHubContext<ControlHub, IControlClient> hubContext, IScreenCapturingService screenCapturingService, ILogger<ScreenCastingService> logger) : IScreenCastingService
{
    public void StartStreaming(IViewer viewer, int frameRate)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        viewer.CapturingContext.FrameRate = frameRate;

        logger.LogInformation("Starting screen streaming for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);

        Task.Run(async () => await StreamScreenDataAsync(viewer, viewer.CapturingContext.CancellationTokenSource.Token), viewer.CapturingContext.CancellationTokenSource.Token);
    }

    public void StopStreaming(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        viewer.Dispose();
    }

    private async Task SendDisplays(IViewer viewer)
    {
        var displays = screenCapturingService.GetDisplays();

        await hubContext.Clients.Client(viewer.ConnectionId).ReceiveDisplays(displays);
    }

    private async Task StreamScreenDataAsync(IViewer viewer, CancellationToken cancellationToken)
    {
        await SendDisplays(viewer);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var delay = 1000 / viewer.CapturingContext.FrameRate;

                var screenData = screenCapturingService.GetNextFrame(viewer.ConnectionId);

                if (screenData != null)
                {
                    await hubContext.Clients.Client(viewer.ConnectionId).ReceiveScreenUpdate(screenData);
                }

                await Task.Delay(delay, cancellationToken);
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
