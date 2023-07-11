using RemoteMaster.Shared.Dto;

namespace RemoteMaster.Server.Abstractions;

public interface IInputSender
{
    void SendMouseCoordinates(MouseMoveDto dto);

    void SendMouseButton(long button, string state, int x, int y);
}