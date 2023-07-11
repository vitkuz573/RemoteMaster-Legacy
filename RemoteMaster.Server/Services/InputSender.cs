using RemoteMaster.Server.Abstractions;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class InputSender : IInputSender
{
    private readonly ILogger<InputSender> _logger;

    public InputSender(ILogger<InputSender> logger)
    {
        _logger = logger;
    }

    public void SendMouseCoordinates(int x, int y, double imgWidth, double imgHeight)
    {
        _logger.LogInformation($"Received mouse coordinates: ({x}, {y}) and image dimensions: ({imgWidth}, {imgHeight})");

        var (absoluteX, absoluteY) = ToAbsoluteCoordinates(x, y, imgWidth, imgHeight);

        var input = new INPUT
        {
            type = INPUT_TYPE.INPUT_MOUSE
        };

        input.Anonymous.mi = new MOUSEINPUT
        {
            dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
            dx = absoluteX,
            dy = absoluteY,
            time = 0,
            mouseData = 0,
            dwExtraInfo = (nuint)GetMessageExtraInfo().Value
        };

        var inputs = new Span<INPUT>(ref input);

        SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    private static (int X, int Y) ToAbsoluteCoordinates(int relativeX, int relativeY, double imgWidth, double imgHeight)
    {
        var absoluteX = (int)(relativeX * 65535 / imgWidth);
        var absoluteY = (int)(relativeY * 65535 / imgHeight);

        return (absoluteX, absoluteY);
    }
}