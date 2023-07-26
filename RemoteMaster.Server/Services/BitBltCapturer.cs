using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RemoteMaster.Shared.Native.Windows;
using RemoteMaster.Shared.Native.Windows.ScreenHelper;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class BitBltCapturer : ScreenCapturer
{
    private readonly Dictionary<string, int> _bitBltScreens = new();
    private readonly object _screenBoundsLock = new();

    public override Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public override Rectangle VirtualScreenBounds { get; protected set; } = SystemInformation.VirtualScreen;

    public override string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    public BitBltCapturer(ILogger<ScreenCapturer> logger) : base(logger)
    {
    }

    protected override void Init()
    {
        _bitBltScreens.Clear();

        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            _bitBltScreens.Add(Screen.AllScreens[i].DeviceName, i);
        }
    }

    public override unsafe byte[]? GetNextFrame()
    {
        lock (_screenBoundsLock)
        {
            try
            {
                if (!DesktopHelper.SwitchToInputDesktop())
                {
                    var errCode = Marshal.GetLastWin32Error();
                    _logger.LogError("Failed to switch to input desktop. Last Win32 error code: {errCode}", errCode);
                }

                var result = GetBitBltFrame();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting next frame.");
                return null;
            }
        }
    }

    private unsafe byte[]? GetBitBltFrame()
    {
        try
        {
            var width = CurrentScreenBounds.Width;
            var height = CurrentScreenBounds.Height;
            var left = CurrentScreenBounds.Left;
            var top = CurrentScreenBounds.Top;

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var memoryGraphics = Graphics.FromImage(bitmap);

            var dc1 = GetDC(HWND.Null);
            var dc2 = (HDC)memoryGraphics.GetHdc();

            BitBlt(dc2, 0, 0, width, height, dc1, left, top, ROP_CODE.SRCCOPY);

            memoryGraphics.ReleaseHdc(dc2);
            ReleaseDC(HWND.Null, dc1);

            return SaveBitmap(bitmap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capturer error in BitBltCapture.");
            return null;
        }
    }

    public override IEnumerable<(string name, bool isPrimary, Size resolution)> GetDisplays()
    {
        return Screen.AllScreens.Select(screen => (screen.DeviceName, screen.Primary, screen.Bounds.Size));
    }

    public override void SetSelectedScreen(string displayName)
    {
        if (displayName == SelectedScreen)
        {
            return;
        }

        if (_bitBltScreens.ContainsKey(displayName))
        {
            SelectedScreen = displayName;
        }
        else
        {
            SelectedScreen = _bitBltScreens.Keys.First();
        }

        RefreshCurrentScreenBounds();
    }

    protected override void RefreshCurrentScreenBounds()
    {
        CurrentScreenBounds = Screen.AllScreens[_bitBltScreens[SelectedScreen]].Bounds;
        RaiseScreenChangedEvent(CurrentScreenBounds);
    }
}
