using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace RemoteMaster.Shared.Native.Windows.ScreenHelper;

public partial class Screen
{
    private class MonitorEnumCallback
    {
        public List<Screen> screens = new();

        public unsafe BOOL Callback(HMONITOR monitor, HDC hdc, RECT* lprcMonitor, LPARAM lparam)
        {
            screens.Add(new Screen(monitor, hdc));

            return true;
        }
    }
}