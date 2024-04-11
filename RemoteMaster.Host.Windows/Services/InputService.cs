// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Dtos;
using Serilog;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public sealed class InputService : IInputService
{
    private bool _disposed;
    private readonly BlockingCollection<Action> _operationQueue;
    private readonly CancellationTokenSource _cts;
    private readonly int _numWorkers;
    private readonly object _ctsLock;
    private readonly ConcurrentBag<INPUT> _inputPool;
    private readonly IDesktopService _desktopService;

    public bool InputEnabled { get; set; } = true;

    public InputService(IDesktopService desktopService, int numWorkers = 4)
    {
        _desktopService = desktopService;
        _operationQueue = [];
        _inputPool = [];
        _cts = new();
        _ctsLock = new();
        _numWorkers = numWorkers;

        StartWorkerThreads();
    }

    private static (double, double) GetAbsolutePercentFromRelativePercent(double percentX, double percentY, IScreenCapturerService screenCapturer)
    {
        var absoluteX = screenCapturer.CurrentScreenBounds.Width * percentX + screenCapturer.CurrentScreenBounds.Left - screenCapturer.VirtualScreenBounds.Left;
        var absoluteY = screenCapturer.CurrentScreenBounds.Height * percentY + screenCapturer.CurrentScreenBounds.Top - screenCapturer.VirtualScreenBounds.Top;

        return (absoluteX / screenCapturer.VirtualScreenBounds.Width, absoluteY / screenCapturer.VirtualScreenBounds.Height);
    }

    private void StartWorkerThreads()
    {
        for (var i = 0; i < _numWorkers; i++)
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
                Log.Error(ex, "Exception occurred during operation processing");
            }
        }
    }

    private void EnqueueOperation(Action operation)
    {
        if (InputEnabled)
        {
            _operationQueue.Add(() =>
            {
                _desktopService.SwitchToInputDesktop();
                operation();
            });
        }
    }

    public void StopProcessing()
    {
        lock (_ctsLock)
        {
            _cts.Cancel();
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

    public void SendMouseInput(MouseInputDto dto, IViewer viewer)
    {
        EnqueueOperation(() =>
        {
            var mouseEventFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK;
            
            var dx = 0;
            var dy = 0;
            
            uint mouseData = 0;

            if (dto.DeltaY.HasValue)
            {
                mouseEventFlags |= MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL;
                mouseData = (uint)(dto.DeltaY.Value < 0 ? -120 : 120);
            }
            else
            {
                var xyPercent = GetAbsolutePercentFromRelativePercent(dto.X.GetValueOrDefault(), dto.Y.GetValueOrDefault(), viewer.ScreenCapturer);
                
                dx = (int)(xyPercent.Item1 * 65535D);
                dy = (int)(xyPercent.Item2 * 65535D);

                if (dto.Button.HasValue && dto.Pressed.HasValue)
                {
                    mouseEventFlags |= dto.Button.Value switch
                    {
                        0 => dto.Pressed.Value ? MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN : MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP,
                        1 => dto.Pressed.Value ? MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEDOWN : MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEUP,
                        2 => dto.Pressed.Value ? MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN : MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP,
                        _ => MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE,
                    };
                }
                else
                {
                    mouseEventFlags |= MOUSE_EVENT_FLAGS.MOUSEEVENTF_MOVE;
                }
            }

            PrepareAndSendInput(INPUT_TYPE.INPUT_MOUSE, dto, (input, _) =>
            {
                input.Anonymous.mi = new MOUSEINPUT
                {
                    dwFlags = mouseEventFlags,
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                };

                return input;
            });
        });
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.KeyCode is (int)VIRTUAL_KEY.VK_LWIN or (int)VIRTUAL_KEY.VK_RWIN)
        {
            return;
        }

        EnqueueOperation(() =>
        {
            PrepareAndSendInput(INPUT_TYPE.INPUT_KEYBOARD, dto, (input, data) =>
            {
                input.Anonymous.ki = new KEYBDINPUT
                {
                    wVk = (VIRTUAL_KEY)data.KeyCode,
                    wScan = 0,
                    time = 0,
                    dwFlags = data.Pressed == false ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
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

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _operationQueue.Dispose();

            lock (_ctsLock)
            {
                _cts.Dispose();
            }
        }

        _disposed = true;
    }

    ~InputService()
    {
        Dispose(false);
    }
}
