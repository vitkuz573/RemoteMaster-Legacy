using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services
{
    public class InputSender : IInputSender
    {
        private bool _disposed = false;
        private readonly BlockingCollection<Action> _operationQueue;
        private CancellationTokenSource _cts;
        private readonly int _numWorkers;
        private readonly object _ctsLock = new();
        private readonly ConcurrentBag<INPUT> _inputPool = new();
        private readonly ILogger<InputSender> _logger;

        public InputSender(ILogger<InputSender> logger, int numWorkers = 4)
        {
            _logger = logger;
            _operationQueue = new BlockingCollection<Action>();
            _cts = new CancellationTokenSource();
            _numWorkers = numWorkers;
            StartWorkerThreads();
        }

        private static (double, double) GetNormalizedCoordinates(double percentX, double percentY, IScreenCapturer screenCapturer)
        {
            var virtualScreenWidth = screenCapturer.VirtualScreenBounds.Width;
            var virtualScreenHeight = screenCapturer.VirtualScreenBounds.Height;
            var currentScreenWidth = screenCapturer.CurrentScreenBounds.Width;
            var currentScreenHeight = screenCapturer.CurrentScreenBounds.Height;

            var normalizedX = (percentX * currentScreenWidth + screenCapturer.CurrentScreenBounds.Left - screenCapturer.VirtualScreenBounds.Left) / virtualScreenWidth * 65535;
            var normalizedY = (percentY * currentScreenHeight + screenCapturer.CurrentScreenBounds.Top - screenCapturer.VirtualScreenBounds.Top) / virtualScreenHeight * 65535;

            return (normalizedX, normalizedY);
        }

        private void StartWorkerThreads()
        {
            lock (_ctsLock)
            {
                for (int i = 0; i < _numWorkers; i++)
                {
                    Task.Factory.StartNew(() =>
                    {
                        CancellationToken token;
                        lock (_ctsLock)
                        {
                            token = _cts.Token;
                        }
                        ProcessQueue(token);
                    }, TaskCreationOptions.LongRunning);
                }
            }
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

        public void EnqueueOperation(Action operation)
        {
            _operationQueue.Add(() =>
            {
                DesktopHelper.SwitchToInputDesktop();
                operation();
            });
        }

        public void StopProcessing()
        {
            lock (_ctsLock)
            {
                if (_cts != null)
                {
                    _cts.Cancel();
                    _cts = null;
                }
            }
        }

        private void PrepareAndSendInput<TInput>(INPUT_TYPE inputType, TInput inputData, Func<INPUT, TInput, INPUT> fillInputData)
        {
            var input = GetInput();

            input.type = inputType;
            input = fillInputData(input, inputData);

            var inputs = new Span<INPUT>(ref input);

            SendInput(inputs, Marshal.SizeOf(typeof(INPUT)));

            ReturnInput(input);
        }


        public void SendMouseCoordinates(MouseMoveDto dto, Viewer viewer)
        {
            EnqueueOperation(() =>
            {
                var (normalizedX, normalizedY) = GetNormalizedCoordinates(dto.X, dto.Y, viewer.ScreenCapturer);

                PrepareAndSendInput(INPUT_TYPE.INPUT_MOUSE, dto, (input, data) =>
                {
                    input.Anonymous.mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
                        dx = (int)normalizedX,
                        dy = (int)normalizedY,
                        time = 0,
                        mouseData = 0,
                        dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                    };

                    return input;
                });
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
                        ButtonAction.Down => MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN,
                        ButtonAction.Up => MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP
                    },
                    1 => dto.State switch
                    {
                        ButtonAction.Down => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEDOWN,
                        ButtonAction.Up => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEUP
                    },
                    2 => dto.State switch
                    {
                        ButtonAction.Down => MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN,
                        ButtonAction.Up => MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP
                    }
                };

                PrepareAndSendInput(INPUT_TYPE.INPUT_MOUSE, dto, (input, data) =>
                {
                    input.Anonymous.mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | mouseEvent | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK,
                        dx = (int)data.X,
                        dy = (int)data.Y
                    };

                    return input;
                });
            });
        }

        public void SendMouseWheel(MouseWheelDto dto)
        {
            EnqueueOperation(() =>
            {
                PrepareAndSendInput(INPUT_TYPE.INPUT_MOUSE, dto, (input, data) =>
                {
                    input.Anonymous.mi = new MOUSEINPUT
                    {
                        dwFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL,
                        dx = 0,
                        dy = 0,
                        time = 0,
                        mouseData = data.DeltaY < 0 ? 120 : data.DeltaY > 0 ? -120 : 0,
                        dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                    };

                    return input;
                });
            });
        }

        public void SendKeyboardInput(KeyboardKeyDto dto)
        {
            EnqueueOperation(() =>
            {
                PrepareAndSendInput(INPUT_TYPE.INPUT_KEYBOARD, dto, (input, data) =>
                {
                    input.Anonymous.ki = new KEYBDINPUT
                    {
                        wVk = (VIRTUAL_KEY)data.Key,
                        wScan = 0,
                        time = 0,
                        dwFlags = data.State == ButtonAction.Up ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
                        dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                    };

                    return input;
                });
            });
        }

        private INPUT GetInput()
        {
            if (!_inputPool.TryTake(out var input))
            {
                input = new INPUT();
            }

            return input;
        }

        private void ReturnInput(INPUT input)
        {
            _inputPool.Add(input);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _operationQueue?.Dispose();
                _cts?.Dispose();
            }

            _disposed = true;
        }

        ~InputSender()
        {
            Dispose(false);
        }
    }
}
