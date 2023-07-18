using Microsoft.Win32;
using System.Drawing;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Interop;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Shared.Native.Windows.ScreenHelper;

public partial class Screen
{
    private readonly IntPtr _hmonitor;

    private readonly Rectangle _bounds;
    private Rectangle _workingArea = Rectangle.Empty;
    private readonly bool _primary;
    private readonly string _deviceName;
    private readonly int _bitDepth;

    private static readonly object s_syncLock = new();

    private static int s_desktopChangedCount = -1;
    private int _currentDesktopChangedCount = -1;

    private const int PRIMARY_MONITOR = unchecked((int)0xBAADF00D);

    private static Screen[]? s_screens;

    internal Screen(IntPtr monitor) : this(monitor, default)
    {
    }

    internal unsafe Screen(IntPtr monitor, HDC hdc)
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
            var info = new User32.MONITORINFOEXW()
            {
                cbSize = (uint)sizeof(User32.MONITORINFOEXW),
            };

            User32.GetMonitorInfoW(monitor, ref info);
            _bounds = info.rcMonitor;
            _primary = (info.dwFlags & User32.MONITORINFOF.PRIMARY) != 0;

            _deviceName = new string(info.szDevice);

            // if (hdc.IsNull)
            // {
            //     // screenDC = CreateDCW(info.szDevice, null, null, null);
            // }
        }

        _hmonitor = monitor;

        _bitDepth = GetDeviceCaps(screenDC, GET_DEVICE_CAPS_INDEX.BITSPIXEL);
        _bitDepth *= GetDeviceCaps(screenDC, GET_DEVICE_CAPS_INDEX.PLANES);

        if (hdc != screenDC)
        {
            // DeleteDC(screenDC);
        }
    }

    public static unsafe Screen[] AllScreens
    {
        get
        {
            if (s_screens == null)
            {
                if (SystemInformation.MultiMonitorSupport)
                {
                    var closure = new MonitorEnumCallback();
                    var proc = new MONITORENUMPROC(closure.Callback);
                    EnumDisplayMonitors(new HDC(), new RECT(), proc, (nint)null);

                    if (closure.screens.Count > 0)
                    {
                        s_screens = closure.screens.ToArray();
                    }
                    else
                    {
                        s_screens = new Screen[] { new Screen(PRIMARY_MONITOR) };
                    }
                }
                else
                {
                    s_screens = new Screen[] { PrimaryScreen! };
                }

                SystemEvents.DisplaySettingsChanging += new EventHandler(OnDisplaySettingsChanging);
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
                return new Screen(PRIMARY_MONITOR);
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
                    var info = new User32.MONITORINFOEXW()
                    {
                        cbSize = (uint)sizeof(User32.MONITORINFOEXW)
                    };

                    User32.GetMonitorInfoW(_hmonitor, ref info);
                    _workingArea = info.rcWork;
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
                        SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(OnUserPreferenceChanged);
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
        ? new Screen(MonitorFromPoint(point, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST))
        : new Screen(PRIMARY_MONITOR);

    public static Screen FromRectangle(RECT rect)
        => SystemInformation.MultiMonitorSupport
        ? new Screen(MonitorFromRect(rect, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST))
        : new Screen(PRIMARY_MONITOR);

    public static Rectangle GetWorkingArea(Point pt) => FromPoint(pt).WorkingArea;

    public static Rectangle GetWorkingArea(Rectangle rect) => FromRectangle(rect).WorkingArea;

    public static Rectangle GetBounds(Point pt) => FromPoint(pt).Bounds;

    public static Rectangle GetBounds(Rectangle rect) => FromRectangle(rect).Bounds;

    public override int GetHashCode() => PARAM.ToInt(_hmonitor);

    private static void OnDisplaySettingsChanging(object? sender, EventArgs e)
    {
        SystemEvents.DisplaySettingsChanging -= new EventHandler(OnDisplaySettingsChanging);
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