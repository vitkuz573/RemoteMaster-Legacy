using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Abstractions;

public interface IInputSender : IDisposable
{
    bool InputEnabled { get; set; }

    void SendMouseCoordinates(MouseMoveDto dto, Viewer viewer);

    void SendMouseButton(MouseClickDto dto, Viewer viewer);

    void SendMouseWheel(MouseWheelDto dto);

    void SendKeyboardInput(KeyboardKeyDto dto);
}