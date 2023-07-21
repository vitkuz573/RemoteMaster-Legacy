using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Abstractions;

public interface IInputSender : IDisposable
{
    void SendMouseCoordinates(MouseMoveDto dto, Viewer viewer);

    void SendMouseButton(MouseButtonClickDto dto);

    void SendMouseWheel(MouseWheelDto dto);

    void SendKeyboardInput(KeyboardKeyDto dto);
}