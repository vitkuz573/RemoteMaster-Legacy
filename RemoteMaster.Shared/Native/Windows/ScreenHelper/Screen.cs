using System.Drawing;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Interop;

namespace RemoteMaster.Shared.Native.Windows.ScreenHelper;

[SupportedOSPlatform("windows6.0.6000")]
public partial class Screen
{
    private readonly HMONITOR _hmonitor;
    private readonly Rectangle _bounds;
    private Rectangle _workingArea = Rectangle.Empty;
    private readonly bool _primary;
    private readonly string _deviceName;
    private readonly int _bitDepth;

    private static readonly object s_syncLock = new();

    private static int s_desktopChangedCount = -1;
    private int _currentDesktopChangedCount = -1;

    private static readonly HMONITOR PRIMARY_MONITOR = (HMONITOR)unchecked((nint)0xBAADF00D);

    private static Screen[]? s_screens;

    internal Screen(HMONITOR monitor) : this(monitor, default)
    {
    }

    internal unsafe Screen(HMONITOR monitor, HDC hdc)
    {
        var screenDC = hdc;

        if (!SystemInformation.MultiMonitorSupport || monitor == PRIMARY_MONITOR)
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

            if (hdc == null)
            {
                screenDC = PInvoke.CreateDCW(_deviceName, null, null, null);
            }
        }

        _hmonitor = monitor;

        _bitDepth = PInvoke.GetDeviceCaps(screenDC, GET_DEVICE_CAPS_INDEX.BITSPIXEL);
        _bitDepth *= PInvoke.GetDeviceCaps(screenDC, GET_DEVICE_CAPS_INDEX.PLANES);

        if (hdc != screenDC)
        {
            PInvoke.DeleteDC(screenDC);
        }
    }

    public static unsafe Screen[] AllScreens
    {
        get
        {
            if (s_screens is null)
            {
                if (SystemInformation.MultiMonitorSupport)
                {
                    List<Screen> screens = new();

                    PInvoke.EnumDisplayMonitors((HMONITOR hmonitor, HDC hdc) =>
                    {
                        screens.Add(new(hmonitor, hdc));
                        return true;
                    });

                    s_screens = screens.Count > 0 ? screens.ToArray() : new Screen[] { new(PRIMARY_MONITOR) };
                }
                else
                {
                    s_screens = new Screen[] { PrimaryScreen! };
                }

                SystemEvents.DisplaySettingsChanging += OnDisplaySettingsChanging;
            }

            return s_screens;
        }
    }

    public int BitsPerPixel => _bitDepth;

    public Rectangle Bounds => _bounds;

    public string DeviceName => _deviceName;

    public bool Primary => _primary;

    public static Screen? PrimaryScreen
    {
        get
        {
            if (SystemInformation.MultiMonitorSupport)
            {
                var screens = AllScreens;

                for (var i = 0; i < screens.Length; i++)
                {
                    if (screens[i]._primary)
                    {
                        return screens[i];
                    }
                }

                return null;
            }
            else
            {
                return new Screen(PRIMARY_MONITOR, default);
            }
        }
    }

    public unsafe Rectangle WorkingArea
    {
        get
        {
            if (_currentDesktopChangedCount != DesktopChangedCount)
            {
                Interlocked.Exchange(ref _currentDesktopChangedCount, DesktopChangedCount);

                if (!SystemInformation.MultiMonitorSupport || _hmonitor == PRIMARY_MONITOR)
                {
                    _workingArea = SystemInformation.WorkingArea;
                }
                else
                {
                    MONITORINFOEXW info = new()
                    {
                        monitorInfo = new() { cbSize = (uint)sizeof(MONITORINFOEXW) }
                    };

                    PInvoke.GetMonitorInfo(_hmonitor, (MONITORINFO*)&info);
                    _workingArea = info.monitorInfo.rcWork;
                }
            }

            return _workingArea;
        }
    }

    private static int DesktopChangedCount
    {
        get
        {
            if (s_desktopChangedCount == -1)
            {
                lock (s_syncLock)
                {
                    if (s_desktopChangedCount == -1)
                    {
                        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

                        s_desktopChangedCount = 0;
                    }
                }
            }

            return s_desktopChangedCount;
        }
    }

    public override bool Equals(object? obj) => obj is Screen comp && _hmonitor == comp._hmonitor;

    public static Screen FromPoint(Point point)
        => SystemInformation.MultiMonitorSupport
        ? new Screen(PInvoke.MonitorFromPoint(point, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST))
        : new Screen(PRIMARY_MONITOR);

    public static Screen FromRectangle(Rectangle rect)
        => SystemInformation.MultiMonitorSupport
        ? new Screen(PInvoke.MonitorFromRect(rect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST))
        : new Screen(PRIMARY_MONITOR, default);

    public static Screen FromHandle(IntPtr hwnd)
        => SystemInformation.MultiMonitorSupport
        ? new Screen(PInvoke.MonitorFromWindow((HWND)hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST))
        : new Screen(PRIMARY_MONITOR, default);

    public static Rectangle GetWorkingArea(Point pt) => FromPoint(pt).WorkingArea;

    public static Rectangle GetWorkingArea(Rectangle rect) => FromRectangle(rect).WorkingArea;

    public static Rectangle GetBounds(Point pt) => FromPoint(pt).Bounds;

    public static Rectangle GetBounds(Rectangle rect) => FromRectangle(rect).Bounds;

    public override int GetHashCode() => PARAM.ToInt(_hmonitor);

    private static void OnDisplaySettingsChanging(object? sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanging -= OnDisplaySettingsChanging;

        s_screens = null;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.Desktop)
        {
            Interlocked.Increment(ref s_desktopChangedCount);
        }
    }

    public override string ToString() => $"{GetType().Name}[Bounds={_bounds} WorkingArea={WorkingArea} Primary={_primary} DeviceName={_deviceName}";
}