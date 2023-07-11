namespace RemoteMaster.Server.Abstractions;

public interface IInputSender
{
    void SendMouseCoordinates(int x, int y);

    void SendMouseButton(long button, string state, int x, int y);
}