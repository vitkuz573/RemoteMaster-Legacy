// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class AudioCapturingService(ILogger<AudioCapturingService> logger) : IAudioCapturingService
{
    private readonly WasapiLoopbackCapture _capture = new();
    private readonly ConcurrentQueue<byte[]> _audioBuffer = new();
    private bool _isRecordingStarted;

    public void StartRecording()
    {
        if (_isRecordingStarted)
        {
            return;
        }

        _capture.DataAvailable += Capture_DataAvailable;
        _capture.StartRecording();
        _isRecordingStarted = true;
    }

    public void StopRecording()
    {
        if (!_isRecordingStarted)
        {
            return;
        }

        _capture.StopRecording();
        _capture.DataAvailable -= Capture_DataAvailable;
        _isRecordingStarted = false;
    }

    private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            var buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);

            _audioBuffer.Enqueue(buffer);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error while capturing audio: {ex.Message}");
        }
    }

    public byte[]? GetNextAudioChunk()
    {
        return _audioBuffer.TryDequeue(out var data) ? data : null;
    }

    public void Dispose()
    {
        if (_isRecordingStarted)
        {
            StopRecording();
        }

        _capture.Dispose();

        _audioBuffer.Clear();
    }
}
