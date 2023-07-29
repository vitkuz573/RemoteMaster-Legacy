using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Core.Abstractions;

public interface IInputSender : IDisposable
{
    bool InputEnabled { get; set; }

    void SendMouseCoordinates(MouseMoveDto dto, IViewer viewer);

    void SendMouseButton(MouseClickDto dto, IViewer viewer);

    void SendMouseWheel(MouseWheelDto dto);

    void SendKeyboardInput(KeyboardKeyDto dto);
}