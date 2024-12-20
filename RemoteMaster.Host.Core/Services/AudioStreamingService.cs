// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Hubs;

namespace RemoteMaster.Host.Core.Services;

public class AudioStreamingService(IHubContext<ControlHub, IControlClient> hubContext, IAudioCapturingService audioCapturingService, ILogger<AudioStreamingService> logger) : IAudioStreamingService
{
    public void StartStreaming(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        logger.LogInformation("Starting audio streaming for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);

        audioCapturingService.StartRecording();

        Task.Run(async () => await StreamAudioDataAsync(viewer, viewer.CancellationTokenSource.Token), viewer.CancellationTokenSource.Token);
    }

    public void StopStreaming(IViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        audioCapturingService.StopRecording();

        viewer.Dispose();
        logger.LogInformation("Stopped audio streaming for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);
    }

    private async Task StreamAudioDataAsync(IViewer viewer, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var audioData = audioCapturingService.GetNextAudioChunk();

                if (audioData != null)
                {
                    var base64 = Convert.ToBase64String(audioData);

                    await hubContext.Clients.Client(viewer.ConnectionId).ReceiveAudioUpdate(base64);
                }

                await Task.Delay(20, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Audio streaming was canceled for connection ID {ConnectionId}, User: {UserName}", viewer.ConnectionId, viewer.UserName);
        }
        catch (Exception ex)
        {
            logger.LogError("An error occurred during audio streaming: {Message}", ex.Message);
        }
    }
}
