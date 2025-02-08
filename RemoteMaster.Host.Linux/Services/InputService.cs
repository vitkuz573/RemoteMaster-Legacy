// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Helpers;
using RemoteMaster.Shared.DTOs;

namespace RemoteMaster.Host.Linux.Services;

public class InputService : IInputService
{
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;
    private nint _display;

    public bool InputEnabled { get; set; } = true;

    public bool BlockUserInput { get; set; }

    public InputService()
    {
        if (!X11Native.XInitThreads())
        {
            throw new Exception("Failed to initialize X threading support (XInitThreads).");
        }

        _display = X11Native.XOpenDisplay(string.Empty);

        if (_display == nint.Zero)
        {
            throw new Exception("Unable to open X display");
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(InputService));
    }

    public void HandleKeyboardInput(KeyboardInputDto dto, string connectionId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!InputEnabled || _disposed)
        {
            return;
        }

        var keysymStr = ConvertInputCodeToKeysym(dto.Code);
        var keysym = X11Native.XStringToKeysym(keysymStr);

        if (keysym == nint.Zero)
        {
            return;
        }

        var keycode = X11Native.XKeysymToKeycode(_display, keysym);

        if (keycode == 0)
        {
            return;
        }

        XtstNative.XTestFakeKeyEvent(_display, keycode, dto.IsPressed, 0);
        X11Native.XFlush(_display);
    }

    public void HandleMouseInput(MouseInputDto dto, string connectionId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!InputEnabled || _disposed)
        {
            return;
        }

        if (dto.DeltaY.HasValue)
        {
            var wheelButton = dto.DeltaY.Value > 0 ? 4u : 5u;
            
            XtstNative.XTestFakeButtonEvent(_display, wheelButton, true, 0);
            XtstNative.XTestFakeButtonEvent(_display, wheelButton, false, 0);
        }
        else if (dto.Position.HasValue)
        {
            var pos = dto.Position.Value;
            var screen = X11Native.XDefaultScreen(_display);
            var absolutePos = pos.X is >= 0 and <= 1 && pos.Y is >= 0 and <= 1
                ? GetAbsoluteCoordinatesFromRelative(pos)
                : new Point((int)pos.X, (int)pos.Y);

            XtstNative.XTestFakeMotionEvent(_display, screen, absolutePos.X, absolutePos.Y, 0);
        }

        if (dto is { Button: not null, IsPressed: not null })
        {
            var button = (uint)(dto.Button.Value + 1);

            XtstNative.XTestFakeButtonEvent(_display, button, dto.IsPressed.Value, 0);
        }

        X11Native.XFlush(_display);
    }

    private static string ConvertInputCodeToKeysym(string code)
    {
        if (code.StartsWith("Key"))
        {
            return code[3..];
        }

        if (code.StartsWith("Digit"))
        {
            return code[5..];
        }

        return code switch
        {
            "Enter" => "Return",
            "ShiftLeft" => "Shift_L",
            "ShiftRight" => "Shift_R",
            "ControlLeft" => "Control_L",
            "ControlRight" => "Control_R",
            "AltLeft" => "Alt_L",
            "AltRight" => "Alt_R",
            "Escape" => "Escape",
            "Space" => "space",
            "Backspace" => "BackSpace",
            _ => code
        };
    }

    private Point GetAbsoluteCoordinatesFromRelative(PointF relative)
    {
        var screen = X11Native.XDefaultScreen(_display);
        var screenWidth = X11Native.XDisplayWidth(_display, screen);
        var screenHeight = X11Native.XDisplayHeight(_display, screen);

        var absX = (int)(relative.X * screenWidth);
        var absY = (int)(relative.Y * screenHeight);

        return new Point(absX, absY);
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
            _cts.Cancel();
            _cts.Dispose();
        }

        if (_display != nint.Zero)
        {
            X11Native.XCloseDisplay(_display);

            _display = nint.Zero;
        }

        _disposed = true;
    }

    ~InputService()
    {
        Dispose(false);
    }
}
