// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Windows.Services;

public class AudioCapturingService : IAudioCapturingService
{
    private readonly ILogger<AudioCapturingService> _logger;

    private readonly WasapiLoopbackCapture _capture;
    private readonly ConcurrentQueue<byte[]> _audioBuffer = new();
    private bool _isRecordingStarted;

    public AudioCapturingService(ILogger<AudioCapturingService> logger)
    {
        _logger = logger;

        _capture = new WasapiLoopbackCapture();

        var waveFormat = _capture.WaveFormat;
        
        _logger.LogInformation($"Audio capture initialized: {waveFormat.SampleRate} Hz, {waveFormat.BitsPerSample}-bit, {waveFormat.Channels} channels");
    }

    public void StartRecording()
    {
        if (_isRecordingStarted)
        {
            _logger.LogWarning("Recording already started.");

            return;
        }

        _capture.DataAvailable += Capture_DataAvailable;
        _capture.StartRecording();
        _isRecordingStarted = true;

        _logger.LogInformation("Audio recording started.");
    }

    public void StopRecording()
    {
        if (!_isRecordingStarted)
        {
            _logger.LogWarning("Recording has not started.");
            return;
        }

        _capture.StopRecording();
        _capture.DataAvailable -= Capture_DataAvailable;
        _isRecordingStarted = false;

        _logger.LogInformation("Audio recording stopped.");
    }

    private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            _logger.LogInformation($"Captured {e.BytesRecorded} bytes of audio data.");

            var buffer = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, buffer, e.BytesRecorded);

            var waveFormat = _capture.WaveFormat;

            _logger.LogInformation($"Captured data: {waveFormat.SampleRate} Hz, {waveFormat.BitsPerSample}-bit, {waveFormat.Channels} channels");

            _audioBuffer.Enqueue(buffer);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error while capturing audio: {ex.Message}");
        }
    }

    public byte[]? GetNextAudioChunk()
    {
        var chunk = _audioBuffer.TryDequeue(out var data) ? data : null;

        _logger.LogInformation(chunk != null
            ? $"Extracted {chunk.Length} bytes of audio data from the buffer."
            : "No audio data available in the buffer.");

        return chunk;
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing audio capturing service.");

        if (_isRecordingStarted)
        {
            StopRecording();
        }

        _capture.Dispose();

        _audioBuffer.Clear();

        _logger.LogInformation("Audio buffer cleared.");
    }
}
