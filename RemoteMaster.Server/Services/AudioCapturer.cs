using NAudio.Wave;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class AudioCapturer : IAudioCapturer
{
    private readonly WasapiLoopbackCapture _capture;
    public event EventHandler<WaveInEventArgs> DataAvailable;

    public AudioCapturer()
    {
        _capture = new WasapiLoopbackCapture();

        _capture.DataAvailable += (s, a) =>
        {
            DataAvailable?.Invoke(s, a);
        };

        _capture.RecordingStopped += (s, a) =>
        {
            _capture?.Dispose();
        };
    }

    public void StartCapturing()
    {
        _capture.StartRecording();
    }

    public void StopCapturing()
    {
        _capture.StopRecording();
    }
}
