namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer
{
    byte[] CaptureScreen();

    IEnumerable<string> GetDisplayNames();
}