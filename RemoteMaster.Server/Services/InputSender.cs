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
    private readonly BlockingCollection<Action> _operationQueue;
    private CancellationTokenSource _cts;
    private readonly int _numWorkers;

    public InputSender(ILogger<InputSender> logger, int numWorkers = 4)
    {
        _logger = logger;
        _operationQueue = new BlockingCollection<Action>();
        _cts = new CancellationTokenSource();
        _numWorkers = numWorkers;
        StartWorkerThreads();
    }

    private void StartWorkerThreads()
    {
        for (int i = 0; i < _numWorkers; i++)
        {
            Task.Factory.StartNew(() => ProcessQueue(_cts.Token), TaskCreationOptions.LongRunning);
        }
    }

    public void EnqueueOperation(Action operation)
    {
        _operationQueue.Add(() =>
        {
            try
            {
                DesktopHelper.SwitchToInputDesktop();
                operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during operation execution");
            }
        });
    }

    private void ProcessQueue(CancellationToken token)
    {
        foreach (var operation in _operationQueue.GetConsumingEnumerable(token))
        {
            try
            {
                operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during operation processing");
            }
        }
    }

    public void StopProcessing()
    {
        _cts.Cancel();
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

    public void SendMouseWheel(MouseWheelDto dto)
    {
        var input = new INPUT
        {
            type = INPUT_TYPE.INPUT_MOUSE,
            Anonymous =
            {
                mi = new MOUSEINPUT
                {
                    dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL,
                    dx = 0,
                    dy = 0,
                    time = 0,
                    mouseData = dto.DeltaY < 0 ? 120 : dto.DeltaY > 0 ? -120 : 0,
                    dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                }
            }
        };

        var inputs = new Span<INPUT>(ref input);

        SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));
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
