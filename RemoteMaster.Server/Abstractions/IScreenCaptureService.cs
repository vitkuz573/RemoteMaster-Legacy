using RemoteMaster.Server.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCaptureService
{
    byte[] CaptureScreen();

    ClientConfig GetClientConfig(string controlId);
}