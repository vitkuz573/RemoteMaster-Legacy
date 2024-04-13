// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extensions.System.Drawing.Common;
using FFMpegCore.Pipes;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Host.Core.Services;

public class ScreenRecorderService(IScreenCapturerService screenCapturerService) : IScreenRecorderService
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private Task _recordingTask = Task.CompletedTask;

    public Task StartRecordingAsync(ScreenRecordingDto screenRecordingRequest)
    {
        ArgumentNullException.ThrowIfNull(screenRecordingRequest);

        _cancellationTokenSource = new CancellationTokenSource();
        _recordingTask = RecordVideo(screenRecordingRequest, _cancellationTokenSource.Token);

        if (screenRecordingRequest.Duration > 0)
        {
            StopRecordingAfterDelay(screenRecordingRequest.Duration);
        }

        return Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        await _recordingTask;

        _cancellationTokenSource = new CancellationTokenSource();
        _recordingTask = Task.CompletedTask;
    }

    private async Task RecordVideo(ScreenRecordingDto screenRecordingRequest, CancellationToken cancellationToken)
    {
        var videoFramesSource = new RawVideoPipeSource(GenerateFrames(cancellationToken))
        {
            FrameRate = 10
        };

        await FFMpegArguments
            .FromPipeInput(videoFramesSource)
            .OutputToFile(screenRecordingRequest.Output, false, options => options
                .WithVideoCodec(VideoCodec.LibX264)
                .WithConstantRateFactor((int)screenRecordingRequest.VideoQuality))
            .ProcessAsynchronously();
    }

    private IEnumerable<IVideoFrame> GenerateFrames(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var frameData = screenCapturerService.GetNextFrame();

            if (frameData == null)
            {
                continue;
            }

            using var stream = new MemoryStream(frameData);
            using var bitmap = new Bitmap(stream);

            yield return new BitmapVideoFrameWrapper(bitmap);
        }
    }

    private async void StopRecordingAfterDelay(uint durationInSeconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(durationInSeconds));
        await StopRecordingAsync();
    }
}
