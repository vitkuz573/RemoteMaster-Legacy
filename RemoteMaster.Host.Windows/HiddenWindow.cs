// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host;

public class HiddenWindow
{
    private const string CLASS_NAME = "HiddenWindowClass";
    private const int RESTART_DELAY = 50;

    private HWND _hwnd;
    private readonly WNDPROC _wndProcDelegate;
    private readonly ILogger<HiddenWindow> _logger;
    private readonly IHostInstanceService _hostService;

    public HiddenWindow(IHostInstanceService hostService, ILogger<HiddenWindow> logger)
    {
        _hostService = hostService;
        _logger = logger;

        _wndProcDelegate = WndProc;
    }

    public void Initialize()
    {
        Task.Run(() =>
        {
            InitializeWindow();
            StartMessageLoop();
        });
    }

    private void InitializeWindow()
    {
        if (!TryRegisterClass(out _))
        {
            _logger.LogError("Failed to register the window class.");

            return;
        }

        _hwnd = CreateHiddenWindow();

        if (_hwnd.IsNull)
        {
            _logger.LogError("Failed to create hidden window.");

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
            _logger.LogError("Failed to register session notifications.");
        }
        else
        {
            _logger.LogInformation("Successfully registered for session notifications.");
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

    public void StopMessageLoop()
    {
        PostMessage(_hwnd, WM_QUIT, new WPARAM(0), new LPARAM(0));
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
        _logger.LogInformation("Received session change notification. Reason: {Reason}", sessionChangeReason);
    }

    private string GetSessionChangeDescription(WPARAM wParam)
    {
        return (ulong)wParam.Value switch
        {
            WTS_CONSOLE_DISCONNECT => HandleSessionChange("A session was disconnected from the console terminal"),
            WTS_CONSOLE_CONNECT => HandleSessionChange("A session was connected to the console terminal"),
            WTS_SESSION_LOCK => HandleSessionChange("A session was locked"),
            WTS_SESSION_UNLOCK => HandleSessionChange("A session was unlocked"),
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
        _hostService.Stop();

        while (_hostService.IsRunning())
        {
            await Task.Delay(RESTART_DELAY);
        }

        _hostService.Start();
    }
}

