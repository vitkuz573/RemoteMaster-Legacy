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

    public void SendMouseCoordinates(int x, int y)
    {
        var input = new INPUT
        {
            type = INPUT_TYPE.INPUT_MOUSE,
            Anonymous =
            {
                mi = new MOUSEINPUT
                {
                    dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
                    dx = x,
                    dy = y,
                    time = 0,
                    mouseData = 0,
                    dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                }
            }
        };

        var inputs = new Span<INPUT>(ref input);

        SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}