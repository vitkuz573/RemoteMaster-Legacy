// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class MessageLoopService : IHostedService
{
    private const string CLASS_NAME = "HiddenWindowClass";
    private const int RESTART_DELAY = 50;

    private HWND _hwnd;
    private readonly WNDPROC _wndProcDelegate;
    private readonly IHostInstanceService _hostInstanceService;

    public MessageLoopService(IHostInstanceService hostInstanceService)
    {
        _hostInstanceService = hostInstanceService;

        _wndProcDelegate = WndProc;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() =>
        {
            InitializeWindow();
            StartMessageLoop();
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        PostMessage(_hwnd, WM_QUIT, new WPARAM(0), new LPARAM(0));

        return Task.CompletedTask;
    }

    private void InitializeWindow()
    {
        if (!TryRegisterClass(out _))
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

    private bool TryRegisterClass(out ushort classAtom)
    {
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = _wndProcDelegate
        };

        unsafe
        {
            fixed (char* pClassName = CLASS_NAME)
            {
                wc.lpszClassName = pClassName;
                classAtom = RegisterClassEx(in wc);
            }
        }

        return classAtom != 0;
    }

    private static unsafe HWND CreateHiddenWindow()
    {
        return CreateWindowEx(0, CLASS_NAME, "", 0, 0, 0, 0, 0, HWND.HWND_MESSAGE, null, null, null);
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
        MSG msg;

        unsafe
        {
            while (GetMessage(out msg, _hwnd, 0, 0))
            {
                TranslateMessage(&msg);
                DispatchMessage(in msg);
            }
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_WTSSESSION_CHANGE)
        {
            LogSessionChange(wParam);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private void LogSessionChange(WPARAM wParam)
    {
        var sessionChangeReason = GetSessionChangeDescription(wParam);
        Log.Information("Received session change notification. Reason: {Reason}", sessionChangeReason);
    }

    private string GetSessionChangeDescription(WPARAM wParam)
    {
        return (ulong)wParam.Value switch
        {
            WTS_CONSOLE_DISCONNECT => HandleSessionChange("A session was disconnected from the console terminal"),
            WTS_CONSOLE_CONNECT => HandleSessionChange("A session was connected to the console terminal"),
            _ => "Unknown session change reason."
        };
    }

    private string HandleSessionChange(string changeDescription)
    {
        RestartHostAsync().Wait();

        return changeDescription;
    }

    private async Task RestartHostAsync()
    {
        _hostInstanceService.Stop();

        while (_hostInstanceService.IsRunning())
        {
            await Task.Delay(RESTART_DELAY);
        }

        _hostInstanceService.Start();
    }
}
