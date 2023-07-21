using System.Drawing;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer
{
    event EventHandler<Rectangle> ScreenChanged;

    Rectangle CurrentScreenBounds { get; }

    Rectangle VirtualScreenBounds { get; }

    string SelectedScreen { get; }

    byte[]? GetNextFrame();

    IEnumerable<(string name, bool isPrimary, Size resolution)> GetDisplays();

    void SetSelectedScreen(string displayName);
}