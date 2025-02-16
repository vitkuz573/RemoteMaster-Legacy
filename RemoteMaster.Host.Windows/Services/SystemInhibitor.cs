// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.System.Power;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public unsafe class WindowsSystemInhibitor(ILogger<WindowsSystemInhibitor> logger) : ISystemInhibitor
{
    private const string ClassName = "HiddenShutdownBlockWindowClass";
    private const uint WM_APP_BLOCK_SHUTDOWN = WM_APP + 1;
    private const uint WM_APP_UNBLOCK_SHUTDOWN = WM_APP + 2;

    private HWND _hwnd;
    private Thread? _messageLoopThread;
    private GCHandle _selfHandle;
    private readonly EventWaitHandle _windowInitializedEvent = new ManualResetEvent(false);
    private bool _isShutdownBlocked;
    private bool _isSleepInhibited;
    private string _blockReason = string.Empty;

    private static readonly delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT> WndProc = &StaticWndProc;

    public void Block(string reason)
    {
        if (string.IsNullOrEmpty(reason))
        {
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));
        }

        if (_messageLoopThread == null)
        {
            _messageLoopThread = new Thread(MessageLoopProc)
            {
                IsBackground = true
            };

            _messageLoopThread.Start();

            _windowInitializedEvent.WaitOne();
        }

        _blockReason = reason;

        if (!_hwnd.IsNull)
        {
            PostMessage(_hwnd, WM_APP_BLOCK_SHUTDOWN, new WPARAM(0), new LPARAM(0));
        }
        else
        {
            logger.LogError("Hidden window is not created; shutdown block not applied.");
        }

        var result = SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
        
        if (result == 0)
        {
            var errorCode = Marshal.GetLastWin32Error();
            
            logger.LogError("Failed to prevent sleep mode, error code: {ErrorCode}", errorCode);
            
            throw new InvalidOperationException($"Error blocking sleep mode, error code: {errorCode}");
        }

        _isSleepInhibited = true;

        logger.LogInformation("System block applied (sleep and shutdown): {Reason}", reason);
    }

    public void Unblock()
    {
        if (!_hwnd.IsNull)
        {
            PostMessage(_hwnd, WM_APP_UNBLOCK_SHUTDOWN, new WPARAM(0), new LPARAM(0));
        }

        if (_isSleepInhibited)
        {
            var result = SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

            if (result == 0)
            {
                var errorCode = Marshal.GetLastWin32Error();
                
                logger.LogError("Failed to release sleep block, error code: {ErrorCode}", errorCode);
            }
            else
            {
                logger.LogInformation("Sleep block released.");
            }

            _isSleepInhibited = false;
        }
    }

    public void Dispose()
    {
        Unblock();

        if (!_hwnd.IsNull)
        {
            PostMessage(_hwnd, WM_CLOSE, new WPARAM(0), new LPARAM(0));
        }

        _messageLoopThread?.Join(3000);

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }

        GC.SuppressFinalize(this);
    }

    private void MessageLoopProc()
    {
        if (!TryRegisterClass())
        {
            logger.LogError("Failed to register window class.");

            return;
        }

        _hwnd = CreateHiddenWindow();

        if (_hwnd.IsNull)
        {
            logger.LogError("Failed to create hidden window.");

            return;
        }

        _selfHandle = GCHandle.Alloc(this);

        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, GCHandle.ToIntPtr(_selfHandle));

        _windowInitializedEvent.Set();

        MSG msg;

        while (GetMessage(out msg, _hwnd, 0, 0))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
    }

    private static bool TryRegisterClass()
    {
        WNDCLASSEXW wc;

        using (var moduleHandle = GetModuleHandle(null))
        {
            wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                hInstance = (HINSTANCE)moduleHandle.DangerousGetHandle(),
            };
        }
        
        fixed (char* classNamePtr = ClassName)
        {
            wc.lpfnWndProc = WndProc;
            wc.lpszClassName = classNamePtr;
        }

        var classAtom = RegisterClassEx(in wc);

        return classAtom != 0;
    }

    private static HWND CreateHiddenWindow()
    {
        return CreateWindowEx(0, ClassName, string.Empty, 0, 0, 0, 0, 0, HWND.HWND_MESSAGE, null, null, null);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT StaticWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        var ptr = GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
        
        if (ptr == 0)
        {
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        var handle = GCHandle.FromIntPtr(ptr);

        if (handle.Target is WindowsSystemInhibitor instance)
        {
            return instance.InstanceWndProc(hWnd, msg, wParam, lParam);
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private LRESULT InstanceWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_APP_BLOCK_SHUTDOWN:
                if (!_isShutdownBlocked)
                {
                    var result = ShutdownBlockReasonCreate(hWnd, _blockReason);

                    if (!result)
                    {
                        var errorCode = Marshal.GetLastWin32Error();

                        logger.LogError("Failed to block shutdown, error code: {ErrorCode}", errorCode);
                    }
                    else
                    {
                        _isShutdownBlocked = true;

                        logger.LogInformation("Shutdown block applied, reason: {Reason}", _blockReason);
                    }
                }

                break;

            case WM_APP_UNBLOCK_SHUTDOWN:
                if (_isShutdownBlocked)
                {
                    var result = ShutdownBlockReasonDestroy(hWnd);

                    if (!result)
                    {
                        var errorCode = Marshal.GetLastWin32Error();

                        logger.LogError("Failed to unblock shutdown, error code: {ErrorCode}", errorCode);
                    }
                    else
                    {
                        _isShutdownBlocked = false;

                        logger.LogInformation("Shutdown block released.");
                    }
                }

                break;

            case WM_CLOSE:
                DestroyWindow(hWnd);
                PostQuitMessage(0);

                break;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
