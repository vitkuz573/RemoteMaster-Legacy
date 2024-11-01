// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Shared.Abstractions;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Host.Windows.Services;

public class TrayIconManager : ITrayIconManager
{
    private const string ClassName = "TrayIconWindowClass";

    private readonly IHostInformationService _hostInformationService;
    private readonly ILogger<TrayIconManager> _logger;

    private HWND _hwnd;
    private DestroyIconSafeHandle _iconHandle;
    private NOTIFYICONDATAW _notifyIconData;
    private readonly WNDPROC _wndProcDelegate;
    private bool _iconAdded;
    private ushort _classAtom;

    public TrayIconManager(IHostInformationService hostInformationService, ILogger<TrayIconManager> logger)
    {
        _hostInformationService = hostInformationService;   
        _logger = logger;

        _wndProcDelegate = WndProc;

        InitializeWindow();
    }

    public void ShowTrayIcon()
    {
        AddTrayIcon();
    }

    public void HideTrayIcon()
    {
        if (!_iconAdded)
        {
            return;
        }

        _logger.LogInformation("Removing tray icon...");

        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, _notifyIconData);

        _iconAdded = false;
    }

    public void UpdateTooltip(string newTooltipText)
    {
        if (!_iconAdded)
        {
            _logger.LogWarning("Tray icon is not added yet. Cannot update tooltip.");

            return;
        }

        _notifyIconData.szTip = newTooltipText;

        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, _notifyIconData);

        _logger.LogInformation("Tray icon tooltip updated to: " + newTooltipText);
    }

    private void InitializeWindow()
    {
        if (!TryRegisterClass())
        {
            _logger.LogError("Failed to register window class for tray icon.");

            return;
        }

        _hwnd = CreateHiddenWindow();

        if (_hwnd.IsNull)
        {
            _logger.LogError("Failed to create hidden window for tray icon.");
        }
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

        _classAtom = RegisterClassEx(wc);

        return _classAtom != 0;
    }

    private static unsafe HWND CreateHiddenWindow()
    {
        return CreateWindowEx(0, ClassName, string.Empty, 0, 0, 0, 0, 0, HWND.HWND_MESSAGE, null, null, null);
    }

    private void AddTrayIcon()
    {
        _iconHandle = ExtractIcon(@"%SystemRoot%\System32\shell32.dll", 15);

        if (_iconHandle.IsInvalid)
        {
            _logger.LogError("Failed to load icon from shell32.dll.");

            return;
        }

        var hostInformation = _hostInformationService.GetHostInformation();

        _notifyIconData = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP,
            uCallbackMessage = 0x400,
            hIcon = (HICON)_iconHandle.DangerousGetHandle(),
            szTip = $"Name: {hostInformation.Name}\r\nIP Address: {hostInformation.IpAddress}\r\nMAC Address: {hostInformation.MacAddress}"
        };

        _iconAdded = Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, _notifyIconData);

        if (_iconAdded)
        {
            _logger.LogInformation("Tray icon successfully added.");
        }
        else
        {
            _logger.LogError("Failed to add tray icon. Shell_NotifyIcon returned false.");
        }
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        HideTrayIcon();

        _iconHandle.Dispose();

        DestroyWindow(_hwnd);

        using var moduleHandle = GetModuleHandle((string)null!);

        UnregisterClass(ClassName, moduleHandle);
    }
}
