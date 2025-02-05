// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Linux.Helpers.ScreenHelper;

/// <summary>
/// Represents a screen (monitor) on Linux.
/// </summary>
public class Screen(Rectangle bounds, string deviceName, bool primary) : IScreen
{
    /// <summary>
    /// Gets the bounds of the screen.
    /// </summary>
    public Rectangle Bounds { get; } = bounds;

    /// <summary>
    /// Gets the device name of the screen.
    /// </summary>
    public string DeviceName { get; } = deviceName;

    /// <summary>
    /// Indicates whether this screen is the primary screen.
    /// </summary>
    public bool Primary { get; } = primary;

    /// <summary>
    /// Retrieves the monitors (screens) from the given X11 display and window.
    /// </summary>
    /// <param name="display">The X11 display handle.</param>
    /// <param name="window">The window to query monitors for (typically the root window).</param>
    /// <returns>An array of <see cref="Screen"/> objects.</returns>
    private static Screen[] GetScreens(nint display, nint window)
    {
        // Retrieve monitor info via XRandr. 'get_active' is set to true.
        var monitorsPtr = XRandrNative.XRRGetMonitors(display, window, true, out var monitorCount);
        var screens = new List<Screen>();

        if (monitorsPtr == nint.Zero || monitorCount <= 0)
        {
            // Fallback: use default screen dimensions from X11.
            var defaultScreen = X11Native.XDefaultScreen(display);
            var width = X11Native.XDisplayWidth(display, defaultScreen);
            var height = X11Native.XDisplayHeight(display, defaultScreen);
            
            screens.Add(new Screen(new Rectangle(0, 0, width, height), "DISPLAY", true));
        }
        else
        {
            var structSize = Marshal.SizeOf<XRandrNative.XRRMonitorInfo>();
            
            for (var i = 0; i < monitorCount; i++)
            {
                var monitorInfoPtr = new nint(monitorsPtr.ToInt64() + i * structSize);
                var monitorInfo = Marshal.PtrToStructure<XRandrNative.XRRMonitorInfo>(monitorInfoPtr);
                var bounds = new Rectangle(monitorInfo.x, monitorInfo.y, monitorInfo.width, monitorInfo.height);
                var deviceName = $"Monitor_{i}";
                var isPrimary = monitorInfo.primary;
                
                screens.Add(new Screen(bounds, deviceName, isPrimary));
            }

            XRandrNative.XRRFreeMonitors(monitorsPtr);
        }

        return [.. screens];
    }

    /// <summary>
    /// Opens the X display, retrieves the screens, and returns both the screens and the display handle.
    /// </summary>
    /// <returns>A tuple containing the screens and the display handle.</returns>
    private static (Screen[] screens, nint display) LoadScreens()
    {
        var display = X11Native.XOpenDisplay(null);
        
        if (display == nint.Zero)
        {
            throw new Exception("Unable to open X display");
        }

        var defaultScreen = X11Native.XDefaultScreen(display);
        var rootWindow = X11Native.XRootWindow(display, defaultScreen);
        var screens = GetScreens(display, rootWindow);
        
        return (screens, display);
    }

    /// <summary>
    /// Gets all screens (monitors) currently detected.
    /// </summary>
    public static IScreen[] AllScreens
    {
        get
        {
            var (screens, display) = LoadScreens();

            X11Native.XCloseDisplay(display);
            
            return screens;
        }
    }

    /// <summary>
    /// Gets the primary screen.
    /// </summary>
    public static IScreen? PrimaryScreen
    {
        get
        {
            var screens = AllScreens;
            
            foreach (var screen in screens)
            {
                if (screen.Primary)
                {
                    return screen;
                }
            }

            return screens.Length > 0 ? screens[0] : null;
        }
    }

    /// <summary>
    /// Gets the virtual screen, which is the bounding rectangle that covers all monitors.
    /// </summary>
    public static IScreen VirtualScreen
    {
        get
        {
            var screens = AllScreens;

            if (screens.Length == 0)
            {
                return new Screen(new Rectangle(0, 0, 0, 0), "VIRTUAL_SCREEN", false);
            }

            var left = screens.Min(s => s.Bounds.Left);
            var top = screens.Min(s => s.Bounds.Top);
            var right = screens.Max(s => s.Bounds.Right);
            var bottom = screens.Max(s => s.Bounds.Bottom);
            
            return new Screen(new Rectangle(left, top, right - left, bottom - top), "VIRTUAL_SCREEN", false);
        }
    }
}
