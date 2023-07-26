using System.Drawing;
using System.Drawing.Imaging;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows.ScreenHelper;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

namespace RemoteMaster.Server.Services;

public class BitBltCapturer : ScreenCapturer
{
    public override Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public override Rectangle VirtualScreenBounds { get; protected set; } = SystemInformation.VirtualScreen;

    public override string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    public BitBltCapturer(ILogger<ScreenCapturer> logger) : base(logger)
    {
    }

    protected override void Init()
    {
        Screens.Clear();

        for (var i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screens.Add(Screen.AllScreens[i].DeviceName, i);
        }
    }

    protected override unsafe byte[]? GetFrame()
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
            _logger.LogError(ex, "Capturer error in GetFrame.");
            return null;
        }
    }

    public override IEnumerable<DisplayInfo> GetDisplays()
    {
        return Screen.AllScreens.Select(screen => new DisplayInfo
        {
            Name = screen.DeviceName,
            IsPrimary = screen.Primary,
            Resolution = screen.Bounds.Size,
        });
    }

    public override void SetSelectedScreen(string displayName)
    {
        if (displayName == SelectedScreen)
        {
            return;
        }

        if (Screens.ContainsKey(displayName))
        {
            SelectedScreen = displayName;
        }
        else
        {
            SelectedScreen = Screens.Keys.First();
        }

        RefreshCurrentScreenBounds();
    }

    protected override void RefreshCurrentScreenBounds()
    {
        CurrentScreenBounds = Screen.AllScreens[Screens[SelectedScreen]].Bounds;
        RaiseScreenChangedEvent(CurrentScreenBounds);
    }
}