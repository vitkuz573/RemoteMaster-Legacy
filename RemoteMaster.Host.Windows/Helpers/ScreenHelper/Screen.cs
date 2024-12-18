// Originally licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licensed the original code under the MIT license.
//
// Adapted by Vitaly Kuzyaev.
// This adapted version is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.Win32;
using RemoteMaster.Host.Core.Abstractions;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace RemoteMaster.Host.Windows.Helpers.ScreenHelper;

public partial class Screen : IScreen
{
    private readonly HMONITOR _hmonitor;

    private readonly Rectangle _bounds;
    private readonly bool _primary;
    private readonly string _deviceName;

    private static readonly HMONITOR s_primaryMonitor = (HMONITOR)unchecked((nint)0xBAADF00D);

    private static IScreen[]? s_screens;

    internal Screen(HMONITOR monitor) : this(monitor, default)
    {
    }

    internal unsafe Screen(HMONITOR monitor, HDC hdc)
    {
        var screenDC = hdc;

        if (!SystemInformation.MultiMonitorSupport || monitor == s_primaryMonitor)
        {
            _bounds = SystemInformation.VirtualScreen;
            _primary = true;
            _deviceName = "DISPLAY";
        }
        else
        {
            MONITORINFOEXW info = new()
            {
                monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFOEXW) }
            };

            PInvoke.GetMonitorInfo(monitor, (MONITORINFO*)&info);
            _bounds = info.monitorInfo.rcMonitor;
            _primary = ((info.monitorInfo.dwFlags & PInvoke.MONITORINFOF_PRIMARY) != 0);

            _deviceName = new string(info.szDevice.ToString());

            if (hdc == default)
            {
                screenDC = PInvoke.CreateDCW(_deviceName, null, null, null);
            }
        }

        _hmonitor = monitor;

        if (hdc != screenDC)
        {
            PInvoke.DeleteDC(screenDC);
        }
    }

    internal Screen(Rectangle bounds, string deviceName, bool primary = false)
    {
        _bounds = bounds;
        _deviceName = deviceName;
        _primary = primary;
    }

    public static unsafe IScreen[] AllScreens
    {
        get
        {
            if (s_screens is null)
            {
                if (SystemInformation.MultiMonitorSupport)
                {
                    List<Screen> screens = [];

                    PInvoke.EnumDisplayMonitors((HMONITOR hmonitor, HDC hdc) =>
                    {
                        screens.Add(new(hmonitor, hdc));
                        return true;
                    });

                    s_screens = screens.Count > 0 ? [.. screens] : [new Screen(s_primaryMonitor)];
                }
                else
                {
                    s_screens = [PrimaryScreen!];
                }

                SystemEvents.DisplaySettingsChanging += OnDisplaySettingsChanging;
            }

            return s_screens;
        }
    }

    public Rectangle Bounds => _bounds;

    public string DeviceName => _deviceName;

    public bool Primary => _primary;

    public static IScreen? PrimaryScreen
    {
        get
        {
            if (SystemInformation.MultiMonitorSupport)
            {
                var screens = AllScreens;

                for (var i = 0; i < screens.Length; i++)
                {
                    if (screens[i].Primary)
                    {
                        return screens[i];
                    }
                }

                return null;
            }
            else
            {
                return new Screen(s_primaryMonitor, default);
            }
        }
    }

    public static IScreen VirtualScreen => new Screen(SystemInformation.VirtualScreen, "VIRTUAL_SCREEN");

    public override bool Equals(object? obj) => obj is Screen comp && _hmonitor == comp._hmonitor;

    public override int GetHashCode() => (int)_hmonitor;

    private static void OnDisplaySettingsChanging(object? sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanging -= OnDisplaySettingsChanging;

        s_screens = null;
    }
}