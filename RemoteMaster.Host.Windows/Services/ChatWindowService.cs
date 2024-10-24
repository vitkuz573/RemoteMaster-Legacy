// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Formatters;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class ChatWindowService(IHostConfigurationService hostConfigurationService, ILogger<ChatWindowService> logger) : IHostedService
{
    private const string ClassName = "ChatWindowClass";

    private const int IDC_CHAT_DISPLAY = 101;
    private const int IDC_CHAT_INPUT = 102;
    private const int IDC_SEND_BUTTON = 103;
    private const int IDC_CONNECTION_STATUS = 104;

    private HWND _hwnd;
    private readonly WNDPROC _wndProcDelegate = WndProc;

    private HubConnection _connection;

    private async Task InitializeSignalRConnectionAsync()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{hostConfiguration.Host.IpAddress}:5001/hubs/chat", options =>
            {
                options.Headers.Add("X-Service-Flag", "true");
            })
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        _connection.Closed += async (error) =>
        {
            UpdateConnectionStatus("Disconnected");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        _connection.Reconnected += (connectionId) =>
        {
            UpdateConnectionStatus("Connected");
            return Task.CompletedTask;
        };

        _connection.Reconnecting += (error) =>
        {
            UpdateConnectionStatus("Reconnecting...");
            return Task.CompletedTask;
        };

        try
        {
            await _connection.StartAsync();
            UpdateConnectionStatus("Connected");
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus($"Failed to connect: {ex.Message}");
        }
    }

    private void UpdateConnectionStatus(string status)
    {
        var connectionStatus = GetDlgItem(_hwnd, IDC_CONNECTION_STATUS);

        if (!connectionStatus.IsNull)
        {
            SetWindowText(connectionStatus, $"Status: {status}");
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        InitializeWindow();

        _ = Task.Run(InitializeSignalRConnectionAsync, cancellationToken);

        StartMessageLoop();

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

        _hwnd = CreateChatWindow();

        if (_hwnd.IsNull)
        {
            logger.LogError("Failed to create hidden window.");
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

        using var safeConnectionStatusHandle = new SafeFileHandle(IDC_CONNECTION_STATUS, false);

        unsafe
        {
            CreateWindowEx(0, "STATIC", "Status: Disconnected", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, 10, 250, 200, 20, _hwnd, safeConnectionStatusHandle, null, null);
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

    private static unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case WM_SETFOCUS:
                var inputField = GetDlgItem(hwnd, IDC_CHAT_INPUT);

                if (!inputField.IsNull)
                {
                    SetFocus(inputField);
                }
                break;

            case WM_COMMAND:
                var wmId = (int)wParam.Value & 0xffff;

                if (wmId == IDC_SEND_BUTTON)
                {
                    var chatInput = GetDlgItem(hwnd, IDC_CHAT_INPUT);
                    var chatDisplay = GetDlgItem(hwnd, IDC_CHAT_DISPLAY);

                    if (!chatInput.IsNull && !chatDisplay.IsNull)
                    {
                        var length = GetWindowTextLength(chatInput);

                        if (length > 0)
                        {
                            var inputBuffer = stackalloc char[length + 1];
                            GetWindowText(chatInput, new PWSTR(inputBuffer), length + 1);
                            var message = new string(inputBuffer, 0, length);

                            var displayLength = GetWindowTextLength(chatDisplay);
                            var displayBuffer = stackalloc char[displayLength + 1];
                            GetWindowText(chatDisplay, new PWSTR(displayBuffer), displayLength + 1);
                            var chatContent = new string(displayBuffer, 0, displayLength);

                            var updatedChatContent = chatContent + "\r\n" + message;
                            SetWindowText(chatDisplay, updatedChatContent);

                            SetWindowText(chatInput, "");
                        }
                    }
                }
                break;
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
