using System.Drawing;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer
{
    event EventHandler<Rectangle> ScreenChanged;

    byte[]? GetNextFrame();

    IEnumerable<string> GetDisplayNames();

    void SetSelectedScreen(string displayName);
}