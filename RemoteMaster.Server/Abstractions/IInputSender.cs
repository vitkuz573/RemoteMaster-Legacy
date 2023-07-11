using RemoteMaster.Shared.Dto;

namespace RemoteMaster.Server.Abstractions;

public interface IInputSender
{
    void SendMouseCoordinates(MouseMoveDto dto);

    void SendMouseButton(MouseButtonClickDto dto);
}