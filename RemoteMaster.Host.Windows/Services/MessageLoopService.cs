// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using RemoteMaster.Host.Windows.Abstractions;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class MessageLoopService : IHostedService
{
    private const string ClassName = "HiddenWindowClass";
    private const int RestartDelay = 50;

    private HWND _hwnd;
    private readonly WNDPROC _wndProcDelegate;
    private readonly ISessionChangeEventService _sessionChangeEventService;

    public MessageLoopService(ISessionChangeEventService sessionChangeEventService)
    {
        _sessionChangeEventService = sessionChangeEventService;
        _wndProcDelegate = WndProc;
    }

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
            Log.Error("Failed to register the window class.");
            return;
        }

        _hwnd = CreateHiddenWindow();

        if (_hwnd.IsNull)
        {
            Log.Error("Failed to create hidden window.");
            return;
        }

        RegisterForSessionNotifications();
    }

    private bool TryRegisterClass()
    {
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = _wndProcDelegate
        };

        unsafe
        {
            fixed (char* pClassName = ClassName)
            {
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
            Log.Error("Failed to register session notifications.");
        }
        else
        {
            Log.Information("Successfully registered for session notifications.");
        }
    }

    private void StartMessageLoop()
    {
        while (GetMessage(out var msg, _hwnd, 0, 0))
        {
            unsafe
            {
                TranslateMessage(&msg);
            }

            DispatchMessage(in msg);
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            _sessionChangeEventService.OnSessionChanged(wParam.Value);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
