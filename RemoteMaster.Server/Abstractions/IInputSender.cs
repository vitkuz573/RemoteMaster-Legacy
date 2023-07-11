namespace RemoteMaster.Server.Abstractions;

public interface IInputSender
{
    void SendMouseCoordinates(int x, int y, double imgWidth, double imgHeight);
}