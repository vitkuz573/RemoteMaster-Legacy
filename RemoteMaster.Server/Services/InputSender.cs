using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dto;
using RemoteMaster.Shared.Native.Windows;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class InputSender : IInputSender
{
    private readonly ILogger<InputSender> _logger;
    private readonly ConcurrentQueue<Action> _operationQueue;
    private Thread _workerThread;
    private CancellationTokenSource _cts;

    public InputSender(ILogger<InputSender> logger)
    {
        _logger = logger;
        _operationQueue = new ConcurrentQueue<Action>();
        _cts = new CancellationTokenSource();
        _workerThread = new Thread(() => ProcessQueue(_cts.Token)) { IsBackground = true };
        _workerThread.Start();
    }

    public void EnqueueOperation(Action operation)
    {
        _operationQueue.Enqueue(() =>
        {
            DesktopHelper.SwitchToInputDesktop();
            operation();
        });
    }

    private void ProcessQueue(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_operationQueue.TryDequeue(out var operation))
            {
                operation();
                Thread.Sleep(10);
            }
            else
            {
                Thread.Sleep(50);
            }
        }
    }

    public void StopProcessing()
    {
        _cts.Cancel();
        _workerThread.Join();
        _workerThread = null;
        _cts = null;
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        EnqueueOperation(() =>
        {
            var input = new INPUT
            {
                type = INPUT_TYPE.INPUT_MOUSE,
                Anonymous =
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
                        dx = dto.X,
                        dy = dto.Y,
                        time = 0,
                        mouseData = 0,
                        dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                    }
                }
            };

            var inputs = new Span<INPUT>(ref input);

            SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
        });
    }

    public void SendMouseButton(MouseButtonClickDto dto)
    {
        EnqueueOperation(() =>
        {
            var mouseEvent = dto.Button switch
            {
                0 => dto.State switch
                {
                    "mousedown" => MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN,
                    "mouseup" => MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP
                },
                1 => dto.State switch
                {
                    "mousedown" => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEDOWN,
                    "mouseup" => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEUP
                },
                2 => dto.State switch
                {
                    "mousedown" => MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN,
                    "mouseup" => MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP
                }
            };

            var input = new INPUT
            {
                type = INPUT_TYPE.INPUT_MOUSE,
                Anonymous =
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | mouseEvent | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
                        dx = dto.X,
                        dy = dto.Y
                    }
                }
            };

            var inputs = new Span<INPUT>(ref input);

            SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
        });
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        EnqueueOperation(() =>
        {
            var input = new INPUT
            {
                type = INPUT_TYPE.INPUT_KEYBOARD,
                Anonymous =
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (VIRTUAL_KEY)dto.Key,
                        wScan = 0,
                        time = 0,
                        dwFlags = dto.State == "keyup" ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
                        dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                    }
                }
            };

            var inputs = new Span<INPUT>(ref input);

            SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
        });
    }
}
