// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class MessageLoopService(ISessionChangeEventService sessionChangeEventService, ILogger<MessageLoopService> logger) : IHostedService
{
    private const string ClassName = "HiddenWindowClass";

    private HWND _hwnd;
    private GCHandle _selfHandle;
    private static readonly unsafe delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT> WndProc = &StaticWndProc;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() =>
        {
            InitializeWindow();
            StartMessageLoop();
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        PostMessage(_hwnd, WM_QUIT, new WPARAM(0), new LPARAM(0));

        return Task.CompletedTask;
    }

    private void InitializeWindow()
    {
        if (!TryRegisterClass())
        {
            logger.LogError("Failed to register the window class.");

            return;
        }

        _hwnd = CreateHiddenWindow();

        if (_hwnd.IsNull)
        {
            logger.LogError("Failed to create hidden window.");

            return;
        }

        _selfHandle = GCHandle.Alloc(this);
        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, (nint)GCHandle.ToIntPtr(_selfHandle));

        RegisterForSessionNotifications();
    }

    private static bool TryRegisterClass()
    {
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
        };

        unsafe
        {
            fixed (char* pClassName = ClassName)
            {
                wc.lpfnWndProc = WndProc;
                wc.lpszClassName = pClassName;
            }
        }

        var classAtom = RegisterClassEx(in wc);

        return classAtom != 0;
    }

    private static unsafe HWND CreateHiddenWindow()
    {
        return CreateWindowEx(0, ClassName, string.Empty, 0, 0, 0, 0, 0, HWND.HWND_MESSAGE, null, null, null);
    }

    private void RegisterForSessionNotifications()
    {
        if (!WTSRegisterSessionNotification(_hwnd, NOTIFY_FOR_ALL_SESSIONS))
        {
            logger.LogError("Failed to register session notifications.");
        }
        else
        {
            logger.LogInformation("Successfully registered for session notifications.");
        }
    }

    private void StartMessageLoop()
    {
        while (GetMessage(out var msg, _hwnd, 0, 0))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT StaticWndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        var ptr = GetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);

        if (ptr == 0)
        {
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        var @this = (MessageLoopService)GCHandle.FromIntPtr(ptr).Target!;

        return @this.InstanceWndProc(hWnd, msg, wParam, lParam);
    }

    private LRESULT InstanceWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            sessionChangeEventService.OnSessionChanged(wParam.Value);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
