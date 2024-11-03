// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Resources;
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

    private DestroyIconSafeHandle _iconHandle;
    private HWND _hwnd;
    private NOTIFYICONDATAW _notifyIconData;
    private readonly WNDPROC _wndProcDelegate;
    private bool _iconAdded;

    private static readonly uint WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

    public TrayIconManager(IHostInformationService hostInformationService, ILogger<TrayIconManager> logger)
    {
        _hostInformationService = hostInformationService;
        _logger = logger;

        _wndProcDelegate = WndProc;

        var defaultIcon = Icons.without_connections;
        _iconHandle = new DestroyIconSafeHandle(defaultIcon.Handle);

        var uiThread = new Thread(() =>
        {
            InitializeWindow();
            StartMessageLoop();
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.IsBackground = true;
        uiThread.Start();
    }

    public void ShowTrayIcon()
    {
        if (!_iconAdded)
        {
            AddTrayIcon();
        }
    }

    public void HideTrayIcon()
    {
        if (_iconAdded && Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_DELETE, _notifyIconData))
        {
            _logger.LogInformation("Tray icon removed successfully.");
            _iconAdded = false;
        }
        else
        {
            _logger.LogWarning("Failed to remove tray icon.");
        }
    }

    public void UpdateIcon(Icon icon)
    {
        if (_hwnd.IsNull)
        {
            _logger.LogWarning("Window handle is invalid. Reinitializing window and tray icon.");
            InitializeWindow();
            ShowTrayIcon();
        }

        ArgumentNullException.ThrowIfNull(icon);

        _iconHandle.Dispose();
        _iconHandle = new DestroyIconSafeHandle(((Icon)icon.Clone()).Handle);

        _notifyIconData.hIcon = (HICON)_iconHandle.DangerousGetHandle();
        _notifyIconData.dwState = 0;
        _notifyIconData.dwStateMask = NOTIFY_ICON_STATE.NIS_HIDDEN;

        if (!Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, _notifyIconData))
        {
            _logger.LogError("Failed to update tray icon. Shell_NotifyIcon returned false. Attempting to reinitialize.");
            _iconAdded = false;
            ShowTrayIcon();
        }
        else
        {
            _logger.LogInformation("Tray icon updated successfully.");
        }
    }

    public void UpdateTooltip(string newTooltipText)
    {
        if (!_iconAdded)
        {
            _logger.LogWarning("Tray icon is not added yet. Cannot update tooltip.");

            return;
        }

        _notifyIconData.szTip = $"Name: {_hostInformationService.GetHostInformation().Name}\n" +
                                $"IP Address: {_hostInformationService.GetHostInformation().IpAddress}\n" +
                                $"MAC Address: {_hostInformationService.GetHostInformation().MacAddress}\n" +
                                $"{newTooltipText}";

        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, _notifyIconData);
    }

    public void UpdateConnectionCount(int activeConnections)
    {
        if (!_iconAdded)
        {
            _logger.LogWarning("Tray icon is not added yet. Cannot update connection count.");
            return;
        }

        _notifyIconData.szTip = GetTooltipText(activeConnections);
        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, _notifyIconData);
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

            return;
        }

        unsafe
        {
            _logger.LogInformation("Hidden window created successfully with handle: {Handle}", (nint)_hwnd.Value);
        }
    }

    private bool TryRegisterClass()
    {
        WNDCLASSEXW wc;

        using (var moduleHandle = GetModuleHandle((string)null!))
        {
            wc = new WNDCLASSEXW
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                lpfnWndProc = _wndProcDelegate,
                hInstance = (HINSTANCE)moduleHandle.DangerousGetHandle(),
            };
        }

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
        return CreateWindowEx(0, ClassName, string.Empty, 0, 0, 0, 0, 0, HWND.Null, null, null, null);
    }

    private void AddTrayIcon()
    {
        if (_hwnd.IsNull)
        {
            _logger.LogWarning("Window handle is invalid. Reinitializing window and tray icon.");
            InitializeWindow();
        }

        _notifyIconData = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_STATE | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP | NOTIFY_ICON_DATA_FLAGS.NIF_INFO,
            uCallbackMessage = 0x400,
            hIcon = (HICON)_iconHandle.DangerousGetHandle(),
            szTip = GetTooltipText(0),
            dwState = 0,
            dwStateMask = NOTIFY_ICON_STATE.NIS_HIDDEN
        };

        _iconAdded = Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, _notifyIconData);

        if (_iconAdded)
        {
            _notifyIconData.Anonymous.uVersion = 4;
            Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, _notifyIconData);

            _logger.LogInformation("Tray icon successfully added.");
        }
        else
        {
            _logger.LogError("Failed to add tray icon. Shell_NotifyIcon returned false.");
        }
    }

    private string GetBaseTooltipText()
    {
        var hostInfo = _hostInformationService.GetHostInformation();
        
        return $"Name: {hostInfo.Name}\nIP Address: {hostInfo.IpAddress}\nMAC Address: {hostInfo.MacAddress}";
    }
    
    private string GetTooltipText(int activeConnections)
    {
        return $"{GetBaseTooltipText()}\nActive Connections: {activeConnections}";
    }

    private LRESULT WndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_TASKBARCREATED)
        {
            _logger.LogInformation("Taskbar was recreated. Restoring tray icon.");
            _iconAdded = false;
            ShowTrayIcon();
        }
        else if (msg == WM_DESTROY || msg == WM_CLOSE)
        {
            _logger.LogWarning("Window handle is invalid. Reinitializing window and tray icon.");
            InitializeWindow();
            ShowTrayIcon();
        }
        else if (msg == WM_MOUSEMOVE || msg == WM_MOUSEHOVER)
        {
            if (!_iconAdded)
            {
                ShowTrayIcon();
            }
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private static void StartMessageLoop()
    {
        while (GetMessage(out var msg, HWND.Null, 0, 0))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
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
