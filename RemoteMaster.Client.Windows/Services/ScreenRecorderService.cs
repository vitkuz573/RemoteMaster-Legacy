// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extensions.System.Drawing.Common;
using FFMpegCore.Pipes;
using RemoteMaster.Client.Core.Abstractions;

namespace RemoteMaster.Client.Services;

public class ScreenRecorderService : IScreenRecorderService
{
    private readonly IScreenCapturerService _screenCapturerService;
    private CancellationTokenSource _cancellationTokenSource;
    private Task _recordingTask;

    public ScreenRecorderService(IScreenCapturerService screenCapturerService)
    {
        _screenCapturerService = screenCapturerService;
    }

    public Task StartRecordingAsync(string outputPath)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _recordingTask = RecordVideo(outputPath, _cancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopRecordingAsync()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            await _recordingTask;
            _cancellationTokenSource = null;
            _recordingTask = null;
        }
    }

    private async Task RecordVideo(string outputPath, CancellationToken cancellationToken)
    {
        var videoFramesSource = new RawVideoPipeSource(GenerateFrames(cancellationToken))
        {
            FrameRate = 30
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

            // Конвертировать frameData в Bitmap
            using var stream = new MemoryStream(frameData);
            using var bitmap = new Bitmap(stream);

            // Обернуть Bitmap с помощью BitmapVideoFrameWrapper
            yield return new BitmapVideoFrameWrapper(bitmap);

            Thread.Sleep((int)(1000.0 / 30));
        }
    }
}
