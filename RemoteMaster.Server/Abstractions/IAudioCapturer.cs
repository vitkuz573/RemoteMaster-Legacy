using NAudio.Wave;

namespace RemoteMaster.Server.Abstractions;

public interface IAudioCapturer
{
    event EventHandler<WaveInEventArgs> DataAvailable;

    void StartCapturing();

    void StopCapturing();
}
