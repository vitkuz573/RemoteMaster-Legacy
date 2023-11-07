// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extensions.System.Drawing.Common;
using FFMpegCore.Pipes;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class ScreenRecorderService : IScreenRecorderService
{
    private readonly IScreenCapturerService _screenCapturerService;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _recordingTask = Task.CompletedTask;

    public ScreenRecorderService(IScreenCapturerService screenCapturerService)
    {
        _screenCapturerService = screenCapturerService;
        _cancellationTokenSource = new();
    }

    public Task StartRecordingAsync(string outputPath)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _recordingTask = RecordVideo(outputPath, _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        if (_recordingTask != null)
        {
            _cancellationTokenSource.Cancel();
            await _recordingTask;
            _cancellationTokenSource = new CancellationTokenSource();
            _recordingTask = Task.CompletedTask;
        }
    }

    private async Task RecordVideo(string outputPath, CancellationToken cancellationToken)
    {
        var videoFramesSource = new RawVideoPipeSource(GenerateFrames(cancellationToken))
        {
            FrameRate = 10
        };

        await FFMpegArguments
            .FromPipeInput(videoFramesSource)
            .OutputToFile(outputPath, false, options => options
                .WithVideoCodec(VideoCodec.LibX264)
                .WithConstantRateFactor(21))
            .ProcessAsynchronously();
    }

    private IEnumerable<IVideoFrame> GenerateFrames(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var frameData = _screenCapturerService.GetNextFrame();

            if (frameData == null)
            {
                continue;
            }

            using var stream = new MemoryStream(frameData);
            using var bitmap = new Bitmap(stream);

            yield return new BitmapVideoFrameWrapper(bitmap);
        }
    }
}
