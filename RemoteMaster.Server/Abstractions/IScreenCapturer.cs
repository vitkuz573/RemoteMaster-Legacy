using System.Drawing;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer
{
    event EventHandler<Rectangle> ScreenChanged;

    Rectangle CurrentScreenBounds { get; }

    Rectangle VirtualScreenBounds { get; }

    string SelectedScreen { get; }

    byte[]? GetNextFrame();

    IEnumerable<string> GetDisplayNames();

    void SetSelectedScreen(string displayName);
}