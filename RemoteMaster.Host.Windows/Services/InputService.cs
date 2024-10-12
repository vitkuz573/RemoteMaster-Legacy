// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.DTOs;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public sealed class InputService(IDesktopService desktopService, ILogger<InputService> logger) : IInputService
{
    private readonly ConcurrentQueue<Action> _operationQueue = new();
    private readonly ManualResetEvent _queueEvent = new(false);
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentBag<INPUT> _inputPool = [];
    private Thread? _workerThread;
    private bool _blockUserInput;
    private bool _disposed;

    public bool InputEnabled { get; set; }

    public bool BlockUserInput
    {
        get => _blockUserInput;
        set
        {
            if (_blockUserInput == value)
            {
                return;
            }

            _blockUserInput = value;
            HandleBlockInput(value);
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(InputService));

        if (_workerThread is { IsAlive: true })
        {
            return;
        }

        _workerThread = new Thread(ProcessQueue)
        {
            IsBackground = true
        };

        _workerThread.Start();
    }

    private static PointF GetAbsolutePercentFromRelativePercent(PointF? position, IScreenCapturingService screenCapturing)
    {
        var absoluteX = screenCapturing.CurrentScreenBounds.Width * position.GetValueOrDefault().X + screenCapturing.CurrentScreenBounds.Left - screenCapturing.VirtualScreenBounds.Left;
        var absoluteY = screenCapturing.CurrentScreenBounds.Height * position.GetValueOrDefault().Y + screenCapturing.CurrentScreenBounds.Top - screenCapturing.VirtualScreenBounds.Top;

        return new PointF(absoluteX / screenCapturing.VirtualScreenBounds.Width, absoluteY / screenCapturing.VirtualScreenBounds.Height);
    }

    private void HandleBlockInput(bool block)
    {
        if (InputEnabled)
        {
            EnqueueOperation(() =>
            {
                desktopService.SwitchToInputDesktop();
                BlockInputAction();
            });
        }
        else
        {
            BlockInputAction();
        }

        return;

        void BlockInputAction()
        {
            if (!BlockInput(block))
            {
                logger.LogError("Failed to block/unblock input. Error code: {ErrorCode}", Marshal.GetLastWin32Error());
            }
        }
    }

    private void ProcessQueue()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            _queueEvent.WaitOne();

            while (_operationQueue.TryDequeue(out var operation))
            {
                try
                {
                    operation();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception occurred during operation processing");
                }
            }

            _queueEvent.Reset();
        }
    }

    private void EnqueueOperation(Action operation)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(InputService));

        if (!InputEnabled)
        {
            return;
        }

        _operationQueue.Enqueue(() =>
        {
            desktopService.SwitchToInputDesktop();
            operation();
        });

        _queueEvent.Set();
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

    public void HandleMouseInput(MouseInputDto dto, IScreenCapturingService screenCapturing)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(InputService));

        EnqueueOperation(() =>
        {
            var mouseEventFlags = MOUSE_EVENT_FLAGS.MOUSEEVENTF_ABSOLUTE | MOUSE_EVENT_FLAGS.MOUSEEVENTF_VIRTUALDESK;

            var dx = 0;
            var dy = 0;

            uint mouseData = 0;

            if (dto.DeltaY.HasValue)
            {
                mouseEventFlags |= MOUSE_EVENT_FLAGS.MOUSEEVENTF_WHEEL;
                mouseData = (uint)(dto.DeltaY.Value < 0 ? 120 : -120);
            }
            else
            {
                var xyPercent = GetAbsolutePercentFromRelativePercent(dto.Position, screenCapturing);

                dx = (int)(xyPercent.X * 65535F);
                dy = (int)(xyPercent.Y * 65535F);

                if (dto is { Button: not null, IsPressed: not null })
                {
                    mouseEventFlags |= dto.Button.Value switch
                    {
                        0 => dto.IsPressed.Value ? MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTDOWN : MOUSE_EVENT_FLAGS.MOUSEEVENTF_LEFTUP,
                        1 => dto.IsPressed.Value ? MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEDOWN : MOUSE_EVENT_FLAGS.MOUSEEVENTF_MIDDLEUP,
                        2 => dto.IsPressed.Value ? MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTDOWN : MOUSE_EVENT_FLAGS.MOUSEEVENTF_RIGHTUP,
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

    public void HandleKeyboardInput(KeyboardInputDto dto)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(InputService));

        ArgumentNullException.ThrowIfNull(dto);

        var virtualKeyCode = ConvertKeyToVirtualKeyCode(dto.Code);

        if (virtualKeyCode is (int)VIRTUAL_KEY.VK_LWIN or (int)VIRTUAL_KEY.VK_RWIN)
        {
            return;
        }

        EnqueueOperation(() =>
        {
            PrepareAndSendInput(INPUT_TYPE.INPUT_KEYBOARD, dto, (input, data) =>
            {
                input.Anonymous.ki = new KEYBDINPUT
                {
                    wVk = (VIRTUAL_KEY)virtualKeyCode,
                    wScan = 0,
                    time = 0,
                    dwFlags = data.IsPressed == false ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : 0,
                    dwExtraInfo = (nuint)GetMessageExtraInfo().Value
                };

                return input;
            });
        });
    }

    private static int ConvertKeyToVirtualKeyCode(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException($"Invalid code: {code}");
        }

        return code switch
        {
            "Digit0" => (int)VIRTUAL_KEY.VK_0,
            "Digit1" => (int)VIRTUAL_KEY.VK_1,
            "Digit2" => (int)VIRTUAL_KEY.VK_2,
            "Digit3" => (int)VIRTUAL_KEY.VK_3,
            "Digit4" => (int)VIRTUAL_KEY.VK_4,
            "Digit5" => (int)VIRTUAL_KEY.VK_5,
            "Digit6" => (int)VIRTUAL_KEY.VK_6,
            "Digit7" => (int)VIRTUAL_KEY.VK_7,
            "Digit8" => (int)VIRTUAL_KEY.VK_8,
            "Digit9" => (int)VIRTUAL_KEY.VK_9,
            "KeyA" => (int)VIRTUAL_KEY.VK_A,
            "KeyB" => (int)VIRTUAL_KEY.VK_B,
            "KeyC" => (int)VIRTUAL_KEY.VK_C,
            "KeyD" => (int)VIRTUAL_KEY.VK_D,
            "KeyE" => (int)VIRTUAL_KEY.VK_E,
            "KeyF" => (int)VIRTUAL_KEY.VK_F,
            "KeyG" => (int)VIRTUAL_KEY.VK_G,
            "KeyH" => (int)VIRTUAL_KEY.VK_H,
            "KeyI" => (int)VIRTUAL_KEY.VK_I,
            "KeyJ" => (int)VIRTUAL_KEY.VK_J,
            "KeyK" => (int)VIRTUAL_KEY.VK_K,
            "KeyL" => (int)VIRTUAL_KEY.VK_L,
            "KeyM" => (int)VIRTUAL_KEY.VK_M,
            "KeyN" => (int)VIRTUAL_KEY.VK_N,
            "KeyO" => (int)VIRTUAL_KEY.VK_O,
            "KeyP" => (int)VIRTUAL_KEY.VK_P,
            "KeyQ" => (int)VIRTUAL_KEY.VK_Q,
            "KeyR" => (int)VIRTUAL_KEY.VK_R,
            "KeyS" => (int)VIRTUAL_KEY.VK_S,
            "KeyT" => (int)VIRTUAL_KEY.VK_T,
            "KeyU" => (int)VIRTUAL_KEY.VK_U,
            "KeyV" => (int)VIRTUAL_KEY.VK_V,
            "KeyW" => (int)VIRTUAL_KEY.VK_W,
            "KeyX" => (int)VIRTUAL_KEY.VK_X,
            "KeyY" => (int)VIRTUAL_KEY.VK_Y,
            "KeyZ" => (int)VIRTUAL_KEY.VK_Z,
            "Enter" => (int)VIRTUAL_KEY.VK_RETURN,
            "ShiftLeft" => (int)VIRTUAL_KEY.VK_LSHIFT,
            "ShiftRight" => (int)VIRTUAL_KEY.VK_RSHIFT,
            "ControlLeft" => (int)VIRTUAL_KEY.VK_LCONTROL,
            "ControlRight" => (int)VIRTUAL_KEY.VK_RCONTROL,
            "AltLeft" => (int)VIRTUAL_KEY.VK_LMENU,
            "AltRight" => (int)VIRTUAL_KEY.VK_RMENU,
            "Escape" => (int)VIRTUAL_KEY.VK_ESCAPE,
            "Tab" => (int)VIRTUAL_KEY.VK_TAB,
            "CapsLock" => (int)VIRTUAL_KEY.VK_CAPITAL,
            "Space" => (int)VIRTUAL_KEY.VK_SPACE,
            "ArrowLeft" => (int)VIRTUAL_KEY.VK_LEFT,
            "ArrowUp" => (int)VIRTUAL_KEY.VK_UP,
            "ArrowRight" => (int)VIRTUAL_KEY.VK_RIGHT,
            "ArrowDown" => (int)VIRTUAL_KEY.VK_DOWN,
            "Insert" => (int)VIRTUAL_KEY.VK_INSERT,
            "Delete" => (int)VIRTUAL_KEY.VK_DELETE,
            "Home" => (int)VIRTUAL_KEY.VK_HOME,
            "End" => (int)VIRTUAL_KEY.VK_END,
            "PageUp" => (int)VIRTUAL_KEY.VK_PRIOR,
            "PageDown" => (int)VIRTUAL_KEY.VK_NEXT,
            "F1" => (int)VIRTUAL_KEY.VK_F1,
            "F2" => (int)VIRTUAL_KEY.VK_F2,
            "F3" => (int)VIRTUAL_KEY.VK_F3,
            "F4" => (int)VIRTUAL_KEY.VK_F4,
            "F5" => (int)VIRTUAL_KEY.VK_F5,
            "F6" => (int)VIRTUAL_KEY.VK_F6,
            "F7" => (int)VIRTUAL_KEY.VK_F7,
            "F8" => (int)VIRTUAL_KEY.VK_F8,
            "F9" => (int)VIRTUAL_KEY.VK_F9,
            "F10" => (int)VIRTUAL_KEY.VK_F10,
            "F11" => (int)VIRTUAL_KEY.VK_F11,
            "F12" => (int)VIRTUAL_KEY.VK_F12,
            "NumLock" => (int)VIRTUAL_KEY.VK_NUMLOCK,
            "ScrollLock" => (int)VIRTUAL_KEY.VK_SCROLL,
            "Numpad0" => (int)VIRTUAL_KEY.VK_NUMPAD0,
            "Numpad1" => (int)VIRTUAL_KEY.VK_NUMPAD1,
            "Numpad2" => (int)VIRTUAL_KEY.VK_NUMPAD2,
            "Numpad3" => (int)VIRTUAL_KEY.VK_NUMPAD3,
            "Numpad4" => (int)VIRTUAL_KEY.VK_NUMPAD4,
            "Numpad5" => (int)VIRTUAL_KEY.VK_NUMPAD5,
            "Numpad6" => (int)VIRTUAL_KEY.VK_NUMPAD6,
            "Numpad7" => (int)VIRTUAL_KEY.VK_NUMPAD7,
            "Numpad8" => (int)VIRTUAL_KEY.VK_NUMPAD8,
            "Numpad9" => (int)VIRTUAL_KEY.VK_NUMPAD9,
            "NumpadMultiply" => (int)VIRTUAL_KEY.VK_MULTIPLY,
            "NumpadAdd" => (int)VIRTUAL_KEY.VK_ADD,
            "NumpadSubtract" => (int)VIRTUAL_KEY.VK_SUBTRACT,
            "NumpadDecimal" => (int)VIRTUAL_KEY.VK_DECIMAL,
            "NumpadDivide" => (int)VIRTUAL_KEY.VK_DIVIDE,
            "Pause" => (int)VIRTUAL_KEY.VK_PAUSE,
            "MetaLeft" => (int)VIRTUAL_KEY.VK_LWIN,
            "MetaRight" => (int)VIRTUAL_KEY.VK_RWIN,
            "ContextMenu" => (int)VIRTUAL_KEY.VK_APPS,
            "BrowserBack" => (int)VIRTUAL_KEY.VK_BROWSER_BACK,
            "BrowserFavorites" => (int)VIRTUAL_KEY.VK_BROWSER_FAVORITES,
            "BrowserForward" => (int)VIRTUAL_KEY.VK_BROWSER_FORWARD,
            "BrowserHome" => (int)VIRTUAL_KEY.VK_BROWSER_HOME,
            "BrowserRefresh" => (int)VIRTUAL_KEY.VK_BROWSER_REFRESH,
            "BrowserSearch" => (int)VIRTUAL_KEY.VK_BROWSER_SEARCH,
            "BrowserStop" => (int)VIRTUAL_KEY.VK_BROWSER_STOP,
            "LaunchApp1" => (int)VIRTUAL_KEY.VK_LAUNCH_APP1,
            "LaunchApp2" => (int)VIRTUAL_KEY.VK_LAUNCH_APP2,
            "LaunchMail" => (int)VIRTUAL_KEY.VK_LAUNCH_MAIL,
            "MediaPlayPause" => (int)VIRTUAL_KEY.VK_MEDIA_PLAY_PAUSE,
            "MediaSelect" => (int)VIRTUAL_KEY.VK_LAUNCH_MEDIA_SELECT,
            "MediaStop" => (int)VIRTUAL_KEY.VK_MEDIA_STOP,
            "MediaTrackNext" => (int)VIRTUAL_KEY.VK_MEDIA_NEXT_TRACK,
            "MediaTrackPrevious" => (int)VIRTUAL_KEY.VK_MEDIA_PREV_TRACK,
            "AudioVolumeDown" => (int)VIRTUAL_KEY.VK_VOLUME_DOWN,
            "AudioVolumeMute" => (int)VIRTUAL_KEY.VK_VOLUME_MUTE,
            "AudioVolumeUp" => (int)VIRTUAL_KEY.VK_VOLUME_UP,
            "PrintScreen" => (int)VIRTUAL_KEY.VK_SNAPSHOT,
            "Backquote" => (int)VIRTUAL_KEY.VK_OEM_3,
            "Backslash" => (int)VIRTUAL_KEY.VK_OEM_5,
            "BracketLeft" => (int)VIRTUAL_KEY.VK_OEM_4,
            "BracketRight" => (int)VIRTUAL_KEY.VK_OEM_6,
            "Comma" => (int)VIRTUAL_KEY.VK_OEM_COMMA,
            "Equal" => (int)VIRTUAL_KEY.VK_OEM_PLUS,
            "Minus" => (int)VIRTUAL_KEY.VK_OEM_MINUS,
            "Period" => (int)VIRTUAL_KEY.VK_OEM_PERIOD,
            "Quote" => (int)VIRTUAL_KEY.VK_OEM_7,
            "Semicolon" => (int)VIRTUAL_KEY.VK_OEM_1,
            "Slash" => (int)VIRTUAL_KEY.VK_OEM_2,
            "Backspace" => (int)VIRTUAL_KEY.VK_BACK,
            "IntlBackslash" => (int)VIRTUAL_KEY.VK_OEM_102,
            "IntlRo" => (int)VIRTUAL_KEY.VK_DBE_SBCSCHAR,
            "IntlYen" => (int)VIRTUAL_KEY.VK_DBE_ROMAN,
            "KanaMode" => (int)VIRTUAL_KEY.VK_KANA,
            "Lang1" => (int)VIRTUAL_KEY.VK_HANJA,
            "Lang2" => (int)VIRTUAL_KEY.VK_KANJI,
            "Convert" => (int)VIRTUAL_KEY.VK_CONVERT,
            "NonConvert" => (int)VIRTUAL_KEY.VK_NONCONVERT,
            "NumpadComma" => (int)VIRTUAL_KEY.VK_OEM_COMMA,
            "NumpadEqual" => (int)VIRTUAL_KEY.VK_OEM_NEC_EQUAL,
            _ => throw new ArgumentException($"Unsupported code: {code}")
        };
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

        if (!disposing)
        {
            return;
        }

        _cts.Cancel();
        _queueEvent.Set();

        if (_workerThread is { IsAlive: true })
        {
            _workerThread.Join();
        }

        _cts.Dispose();
        _queueEvent.Dispose();

        _disposed = true;
    }

    ~InputService()
    {
        Dispose(false);
    }
}
