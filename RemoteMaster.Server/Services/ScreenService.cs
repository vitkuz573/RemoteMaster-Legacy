using RemoteMaster.Server.Abstractions;
using ScreenHelper;
using System.Drawing;

namespace RemoteMaster.Server.Services;

public class ScreenService : IScreenService
{
    public Size GetScreenSize()
    {
        var screen = Screen.PrimaryScreen.Bounds;

        return new Size { Width = screen.Width, Height = screen.Height };
    }

    public Size GetVirtualScreenSize()
    {
        var virtualScreen = SystemInformation.VirtualScreen;

        return new Size { Width = virtualScreen.Width, Height = virtualScreen.Height };
    }
}