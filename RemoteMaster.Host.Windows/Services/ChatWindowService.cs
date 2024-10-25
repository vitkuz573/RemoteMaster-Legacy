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
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Formatters;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class ChatWindowService : IHostedService
{
    private const string ClassName = "ChatWindowClass";

    private const int IDC_CHAT_DISPLAY = 101;
    private const int IDC_CHAT_INPUT = 102;
    private const int IDC_SEND_BUTTON = 103;
    private const int IDC_CONNECTION_STATUS = 104;
    private const int IDC_TYPING_STATUS = 105;

    private HWND _hwnd;
    private GCHandle _gch;

    private HubConnection? _connection;

    private readonly List<ChatMessageDto> _chatMessages = [];

    private Timer? _typingTimer;
    private readonly TimeSpan TypingTimeout = TimeSpan.FromSeconds(2);
    private bool _isTyping = false;
    private const string UserName = "User";

    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<ChatWindowService> _logger;

    private readonly SUBCLASSPROC _inputWndProc;
    private readonly WNDPROC _wndProcDelegate;

    public ChatWindowService(IHostConfigurationService hostConfigurationService, IHostApplicationLifetime applicationLifetime, ILogger<ChatWindowService> logger)
    {
        _hostConfigurationService = hostConfigurationService;
        _applicationLifetime = applicationLifetime;
        _logger = logger;

        _inputWndProc = new SUBCLASSPROC(InputProc);
        _wndProcDelegate = new WNDPROC(WndProc);
    }

    private async Task InitializeSignalRConnectionAsync()
    {
        var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{hostConfiguration.Host.IpAddress}:5001/hubs/chat")
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        _connection.On<ChatMessageDto>("ReceiveMessage", AddMessageToChatDisplay);
        _connection.On<string>("MessageDeleted", RemoveMessageFromChatDisplay);
        _connection.On<string>("UserTyping", ShowTypingIndicator);
        _connection.On<string>("UserStopTyping", _ => HideTypingIndicator());

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

    private async Task NotifyTypingAsync()
    {
        if (_connection == null)
        {
            return;
        }

        _typingTimer?.Change(Timeout.Infinite, Timeout.Infinite);

        if (!_isTyping)
        {
            _isTyping = true;

            try
            {
                await _connection.SendAsync("Typing", UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending typing notification. Exception: {ExceptionMessage}", ex.Message);
            }
        }

        _typingTimer = new Timer(async _ => await NotifyStopTypingAsync(), null, TypingTimeout, Timeout.InfiniteTimeSpan);
    }

    private async Task NotifyStopTypingAsync()
    {
        if (_connection == null || !_isTyping)
        {
            return;
        }

        _isTyping = false;

        try
        {
            await _connection.SendAsync("StopTyping", UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error sending stop typing notification. Exception: {ExceptionMessage}", ex.Message);
        }
    }

    private void AddMessageToChatDisplay(ChatMessageDto chatMessageDto)
    {
        var chatDisplay = GetDlgItem(_hwnd, IDC_CHAT_DISPLAY);

        if (!chatDisplay.IsNull)
        {
            _chatMessages.Add(chatMessageDto);

            var formattedMessage = $"{chatMessageDto.User}: {chatMessageDto.Message}\r\n";

            var currentLength = SendMessage(chatDisplay, WM_GETTEXTLENGTH, new WPARAM(), new LPARAM());

            SendMessage(chatDisplay, EM_SETSEL, new WPARAM((nuint)currentLength.Value), new LPARAM(currentLength.Value));

            var messagePtr = Marshal.StringToHGlobalUni(formattedMessage);
            SendMessage(chatDisplay, EM_REPLACESEL, new WPARAM(0), new LPARAM(messagePtr));
            Marshal.FreeHGlobal(messagePtr);
        }
        else
        {
            _logger.LogError("Failed to access chat display window.");
        }
    }

    private void RemoveMessageFromChatDisplay(string messageId)
    {
        var chatDisplay = GetDlgItem(_hwnd, IDC_CHAT_DISPLAY);

        if (!chatDisplay.IsNull)
        {
            var messageToRemove = _chatMessages.FirstOrDefault(m => m.Id == messageId);

            if (messageToRemove != null)
            {
                _chatMessages.Remove(messageToRemove);

                SetWindowText(chatDisplay, "");

                foreach (var message in _chatMessages)
                {
                    var formattedMessage = $"{message.User}: {message.Message}\r\n";

                    var currentLength = SendMessage(chatDisplay, WM_GETTEXTLENGTH, new WPARAM(), new LPARAM());
                    SendMessage(chatDisplay, EM_SETSEL, new WPARAM((nuint)currentLength.Value), new LPARAM((nint)currentLength.Value));

                    var messagePtr = Marshal.StringToHGlobalUni(formattedMessage);
                    SendMessage(chatDisplay, EM_REPLACESEL, new WPARAM(0), new LPARAM(messagePtr));
                    Marshal.FreeHGlobal(messagePtr);
                }
            }
        }
        else
        {
            _logger.LogError("Failed to access chat display window.");
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
        var uiThread = new Thread(() =>
        {
            InitializeWindow();
            StartMessageLoop();

            _applicationLifetime.StopApplication();
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.IsBackground = true;
        uiThread.Start();

        _ = Task.Run(InitializeSignalRConnectionAsync, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        if (_gch.IsAllocated)
        {
            _gch.Free();
        }
    }

    private void InitializeWindow()
    {
        if (!_hwnd.IsNull)
        {
            _logger.LogInformation("Window already exists, skipping initialization.");
            return;
        }

        if (!TryRegisterClass())
        {
            _logger.LogError("Failed to register the window class.");
            throw new InvalidOperationException("Window class registration failed.");
        }

        _hwnd = CreateChatWindow();

        if (_hwnd.IsNull)
        {
            _logger.LogError("Failed to create the window.");
            throw new InvalidOperationException("Window creation failed.");
        }

        _gch = GCHandle.Alloc(this);
        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, GCHandle.ToIntPtr(_gch));

        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, new nint(GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE) | (int)WINDOW_STYLE.WS_SYSMENU));
        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, new nint(GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE) & ~(int)WINDOW_STYLE.WS_MAXIMIZEBOX));
        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, new nint(GetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE) & ~(int)WINDOW_STYLE.WS_THICKFRAME));

        ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOW);

        InitializeControls();
    }

    private LRESULT InputProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        switch (msg)
        {
            case WM_KEYDOWN:
                if (wParam.Value == (nuint)VIRTUAL_KEY.VK_RETURN)
                {
                    _ = HandleSendButtonAsync();
                    
                    return new LRESULT(0);
                }

                break;
        }

        return DefSubclassProc(hwnd, msg, wParam, lParam);
    }

    private void InitializeControls()
    {
        using var font = CreateDefaultFont();

        using var safeChatDisplayHandle = new SafeFileHandle(IDC_CHAT_DISPLAY, false);

        unsafe
        {
            var chatDisplay = CreateWindowEx(0, "EDIT", "", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_BORDER | WINDOW_STYLE.WS_VSCROLL | (WINDOW_STYLE)ES_MULTILINE | (WINDOW_STYLE)ES_AUTOVSCROLL | (WINDOW_STYLE)ES_READONLY, 10, 10, 300, 200, _hwnd, safeChatDisplayHandle, null, null);

            SendMessage(chatDisplay, WM_SETFONT, new WPARAM((nuint)font.DangerousGetHandle()), new LPARAM(1));
        }

        using var safeChatInputHandle = new SafeFileHandle(IDC_CHAT_INPUT, false);

        unsafe
        {
            var chatInput = CreateWindowEx(0, "EDIT", "", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (WINDOW_STYLE)ES_AUTOHSCROLL | WINDOW_STYLE.WS_BORDER, 10, 220, 200, 20, _hwnd, safeChatInputHandle, null, null);

            SetWindowSubclass(chatInput, _inputWndProc, 0, 0);
            _logger.LogInformation("Subclass applied to chat input.");
        }

        using var safeSendButtonHandle = new SafeFileHandle(IDC_SEND_BUTTON, false);

        unsafe
        {
            CreateWindowEx(0, "BUTTON", "Send", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE | (WINDOW_STYLE)BS_CENTER, 220, 220, 80, 20, _hwnd, safeSendButtonHandle, null, null);
        }

        using var safeConnectionStatusHandle = new SafeFileHandle(IDC_CONNECTION_STATUS, false);

        unsafe
        {
            CreateWindowEx(0, "STATIC", "Status: Disconnected", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, 10, 250, 200, 20, _hwnd, safeConnectionStatusHandle, null, null);
        }

        using var safeTypingStatusHandle = new SafeFileHandle(IDC_TYPING_STATUS, false);

        unsafe
        {
            CreateWindowEx(0, "STATIC", "", WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_VISIBLE, 10, 280, 200, 20, _hwnd, safeTypingStatusHandle, null, null);
        }
    }

    private static DeleteObjectSafeHandle CreateDefaultFont()
    {
        return CreateFont(18, 0, 0, 0, (int)FONT_WEIGHT.FW_NORMAL, 0, 0, 0, (uint)FONT_CHARSET.ANSI_CHARSET, (uint)FONT_OUTPUT_PRECISION.OUT_DEFAULT_PRECIS, (uint)FONT_CLIP_PRECISION.CLIP_DEFAULT_PRECIS, (uint)FONT_QUALITY.CLEARTYPE_QUALITY, (uint)FONT_PITCH.DEFAULT_PITCH | (uint)FONT_FAMILY.FF_SWISS, "Segoe UI");
    }

    private void ShowTypingIndicator(string user)
    {
        var typingStatus = GetDlgItem(_hwnd, IDC_TYPING_STATUS);

        if (!typingStatus.IsNull)
        {
            SetWindowText(typingStatus, $"{user} is typing...");
            ShowWindow(typingStatus, SHOW_WINDOW_CMD.SW_SHOW);
        }
    }

    private void HideTypingIndicator()
    {
        var typingStatus = GetDlgItem(_hwnd, IDC_TYPING_STATUS);

        if (!typingStatus.IsNull)
        {
            SetWindowText(typingStatus, "");
            ShowWindow(typingStatus, SHOW_WINDOW_CMD.SW_HIDE);
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
        const int windowWidth = 335;
        const int windowHeight = 350;

        var hwnd = CreateWindowEx(WINDOW_EX_STYLE.WS_EX_TOPMOST, ClassName, "RemoteMaster Chat", WINDOW_STYLE.WS_OVERLAPPED | WINDOW_STYLE.WS_CAPTION, CW_USEDEFAULT, CW_USEDEFAULT, windowWidth, windowHeight, HWND.Null, null, null, null);

        if (hwnd.IsNull)
        {
            throw new InvalidOperationException("Failed to create window. Handle is not initialized.");
        }

        return hwnd;
    }

    private static unsafe void StartMessageLoop()
    {
        MSG msg;

        while (GetMessage(&msg, HWND.Null, 0, 0))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    private async Task HandleSendButtonAsync()
    {
        var chatInput = GetDlgItem(_hwnd, IDC_CHAT_INPUT);

        if (chatInput.IsNull)
        {
            _logger.LogError("Failed to access chat input window.");
            return;
        }

        var length = GetWindowTextLength(chatInput);

        if (length > 0)
        {
            string message;

            unsafe
            {
                var inputBuffer = stackalloc char[length + 1];

                if (GetWindowText(chatInput, new PWSTR(inputBuffer), length + 1) > 0)
                {
                    message = new string(inputBuffer, 0, length);
                }
                else
                {
                    _logger.LogError("Failed to retrieve text from the input field.");
                    return;
                }
            }

            var chatMessageDto = new ChatMessageDto(UserName, message);

            try
            {
                await _connection.SendAsync("SendMessage", chatMessageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending message via SignalR. Exception: {ExceptionMessage}", ex.Message);
                return;
            }

            SetWindowText(chatInput, "");
        }
    }

    private static int HIWORD(nint n) => (int)((n >> 16) & 0xFFFF);

    private static unsafe LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (hwnd.IsNull)
        {
            throw new InvalidOperationException("Handle is not initialized.");
        }

        var serviceHandle = GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);

        if (serviceHandle == nint.Zero)
        {
            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        var service = (ChatWindowService)GCHandle.FromIntPtr(serviceHandle).Target!;

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
                    _ = service.HandleSendButtonAsync();
                }
                else if (wmId == IDC_CHAT_INPUT && HIWORD((nint)wParam.Value) == EN_CHANGE)
                {
                    _ = service.NotifyTypingAsync();
                }
                break;

            case WM_CLOSE:
                DestroyWindow(hwnd);
                service._hwnd = HWND.Null;
                return new LRESULT(0);

            case WM_DESTROY:
                PostQuitMessage(0);
                return new LRESULT(0);
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}
