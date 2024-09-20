// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32.SafeHandles;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class ChatWindowService : IHostedService
{
    private const string ClassName = "ChatWindowClass";

    private const int IDC_CHAT_DISPLAY = 101;
    private const int IDC_CHAT_INPUT = 102;
    private const int IDC_SEND_BUTTON = 103;

    private HWND _hwnd;
    private readonly WNDPROC _wndProcDelegate = WndProc;

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

        _hwnd = CreateChatWindow();

        if (_hwnd.IsNull)
        {
            Log.Error("Failed to create hidden window.");
        }

        ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);

        using var safeChatDisplayHandle = new SafeFileHandle(IDC_CHAT_DISPLAY, false);

        unsafe
        {
            CreateWindowEx(0, "EDIT", "", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (WINDOW_STYLE)ES_MULTILINE | (WINDOW_STYLE)ES_AUTOVSCROLL | WINDOW_STYLE.WS_VSCROLL | (WINDOW_STYLE)ES_READONLY, 10, 10, 300, 200, _hwnd, safeChatDisplayHandle, null, null);
        }

        using var safeChatInputHandle = new SafeFileHandle(IDC_CHAT_INPUT, false);

        unsafe
        {
            CreateWindowEx(0, "EDIT", "", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (WINDOW_STYLE)ES_AUTOHSCROLL, 10, 220, 200, 20, _hwnd, safeChatInputHandle, null, null);
        }

        using var safeSendButtonHandle = new SafeFileHandle(IDC_SEND_BUTTON, false);

        unsafe
        {
            CreateWindowEx(0, "BUTTON", "Send", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, 220, 220, 80, 20, _hwnd, safeSendButtonHandle, null, null);
        }
    }

    private bool TryRegisterClass()
    {
#pragma warning disable CA2000
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = _wndProcDelegate,
            hInstance = (HINSTANCE)GetModuleHandle((string)null!).DangerousGetHandle(),
        };
#pragma warning restore CA2000

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

    private static unsafe HWND CreateChatWindow()
    {
        const int windowWidth = 500;
        const int windowHeight = 400;

        return CreateWindowEx(0, ClassName, "RemoteMaster Chat", WINDOW_STYLE.WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, windowWidth, windowHeight, HWND.Null, null, null, null);
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

    private static LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_SETFOCUS:
                var inputField = FindWindowEx(hwnd, HWND.Null, "EDIT", null);

                if (!inputField.IsNull)
                {
                    SetFocus(inputField);
                }

                break;
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
