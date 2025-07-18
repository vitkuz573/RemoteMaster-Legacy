﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Resources;
using RemoteMaster.Host.Windows.Enums;
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

    private readonly IAppState _appState;
    private readonly IHostInformationService _hostInformationService;
    private readonly IApplicationVersionProvider _applicationVersionProvider;
    private readonly ILogger<TrayIconManager> _logger;

    private static readonly unsafe delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT> WndProc = &StaticWndProc;
    private GCHandle _selfHandle;
    private readonly SafeHandle _moduleHandle;
    private DestroyIconSafeHandle _iconHandle;
    private DestroyMenuSafeHandle? _trayMenu;

    private HWND _hwnd;
    private NOTIFYICONDATAW _notifyIconData;
    private bool _iconAdded;

    private static readonly uint WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

    public TrayIconManager(IAppState appState, IHostInformationService hostInformationService, IApplicationVersionProvider applicationVersionProvider, ILogger<TrayIconManager> logger)
    {
        _appState = appState;
        _hostInformationService = hostInformationService;
        _applicationVersionProvider = applicationVersionProvider;
        _logger = logger;

        _appState.ViewerAdded += OnViewerChanged;
        _appState.ViewerRemoved += OnViewerChanged;

        _moduleHandle = GetModuleHandle(null) ?? throw new InvalidOperationException("Failed to retrieve module handle.");

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

    public void Show()
    {
        if (!_iconAdded)
        {
            AddTrayIcon();
        }
    }

    public void Hide()
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

    public void SetIcon(Icon icon)
    {
        if (_hwnd.IsNull)
        {
            _logger.LogWarning("Window handle is invalid. Reinitializing window and tray icon.");
            InitializeWindow();
            Show();
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
            Show();
        }
        else
        {
            _logger.LogInformation("Tray icon updated successfully.");
        }
    }

    public void SetTooltip(string tooltip)
    {
        if (!_iconAdded)
        {
            _logger.LogWarning("Tray icon is not added yet. Cannot update tooltip.");

            return;
        }

        string baseTooltipText;

        try
        {
            var hostInfo = _hostInformationService.GetHostInformation();

            baseTooltipText = $"Name: {hostInfo.Name}\nIP Address: {hostInfo.IpAddress}\nMAC Address: {hostInfo.MacAddress}";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve host information. Tooltip will be partially updated.");

            baseTooltipText = "Host information unavailable";
        }

        _notifyIconData.szTip = $"{baseTooltipText}\n{tooltip}";

        Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_MODIFY, _notifyIconData);
    }

    private void UpdateConnectionCount(int activeConnections)
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

        _selfHandle = GCHandle.Alloc(this);
        SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, GCHandle.ToIntPtr(_selfHandle));

        unsafe
        {
            _logger.LogInformation("Hidden window created successfully with handle: {Handle}", (nint)_hwnd.Value);
        }
    }

    private bool TryRegisterClass()
    {
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            hInstance = (HINSTANCE)_moduleHandle.DangerousGetHandle(),
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
            uCallbackMessage = WM_RBUTTONUP,
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
        try
        {
            var hostInfo = _hostInformationService.GetHostInformation();

            return $"Name: {hostInfo.Name}\nIP Address: {hostInfo.IpAddress}\nMAC Address: {hostInfo.MacAddress}";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve host information. Returning placeholder text for tooltip.");

            return "Host information unavailable";
        }
    }

    private string GetTooltipText(int activeConnections)
    {
        return $"{GetBaseTooltipText()}\nActive Connections: {activeConnections}";
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        var ptr = GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);

        if (ptr == 0)
        {
            return DefWindowProc(hwnd, msg, wParam, lParam);
        }

        var manager = (TrayIconManager)GCHandle.FromIntPtr(ptr).Target!;

        return manager.InstanceWndProc(hwnd, msg, wParam, lParam);
    }

    private LRESULT InstanceWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == WM_TASKBARCREATED)
        {
            _logger.LogInformation("Taskbar was recreated. Restoring tray icon.");
            _iconAdded = false;

            Show();
        }
        else if (msg == WM_RBUTTONUP)
        {
            var eventCode = (uint)LOWORD((uint)lParam.Value);

            switch (eventCode)
            {
                case WM_CONTEXTMENU:
                    _logger.LogInformation("Right-click event received, showing context menu.");
                    ShowContextMenu();
                    break;
            }
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    private void CreateContextMenu()
    {
        _trayMenu = CreatePopupMenu_SafeHandle();

        var versionText = $"Version: {_applicationVersionProvider.GetVersionFromAssembly()}";
        AppendMenu(_trayMenu, MENU_ITEM_FLAGS.MF_STRING | MENU_ITEM_FLAGS.MF_DISABLED | MENU_ITEM_FLAGS.MF_GRAYED, 0, versionText);

        // AppendMenu(_trayMenu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, null);
        // 
        // AppendMenu(_trayMenu, MENU_ITEM_FLAGS.MF_STRING, (uint)TrayIconCommands.Open, "Open");
        // AppendMenu(_trayMenu, MENU_ITEM_FLAGS.MF_STRING, (uint)TrayIconCommands.Restart, "Restart");
        // AppendMenu(_trayMenu, MENU_ITEM_FLAGS.MF_STRING, (uint)TrayIconCommands.Exit, "Exit");
    }

    private void ShowContextMenu()
    {
        if (_trayMenu == null || _trayMenu.IsInvalid)
        {
            CreateContextMenu();
        }

        GetCursorPos(out var cursorPos);
        SetForegroundWindow(_hwnd);

        var command = (uint)(int)TrackPopupMenu(_trayMenu, TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD, cursorPos.X, cursorPos.Y, _hwnd, null);

        PostMessage(_hwnd, WM_NULL, 0, 0);

        if (command != 0)
        {
            HandleMenuCommand((TrayIconCommands)command);
        }
    }

    private static void HandleMenuCommand(TrayIconCommands command)
    {
        switch (command)
        {
            case TrayIconCommands.Open:
                break;
            case TrayIconCommands.Restart:
                break;
            case TrayIconCommands.Exit:
                // Dispose();
                // Environment.Exit(0);
                break;
        }
    }

    private static void StartMessageLoop()
    {
        while (GetMessage(out var msg, HWND.Null, 0, 0))
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
    }

    private void OnViewerChanged(object? sender, IViewer? viewer)
    {
        var activeConnections = _appState.GetAllViewers().Count;
        var icon = activeConnections > 0
            ? Icons.with_connections
            : Icons.without_connections;

        UpdateConnectionCount(activeConnections);
        SetIcon(icon);
    }

    public void Dispose()
    {
        Hide();

        _iconHandle.Dispose();

        DestroyWindow(_hwnd);

        if (!_moduleHandle.IsInvalid)
        {
            UnregisterClass(ClassName, _moduleHandle);
        }

        _moduleHandle.Dispose();
    }
}
