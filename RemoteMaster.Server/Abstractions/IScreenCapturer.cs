using System.Drawing;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer : IDisposable
{
    event EventHandler<Rectangle> ScreenChanged;

    Rectangle CurrentScreenBounds { get; }

    Rectangle VirtualScreenBounds { get; }

    string SelectedScreen { get; }

    byte[]? GetNextFrame();

    IEnumerable<DisplayInfo> GetDisplays();

    void SetSelectedScreen(string displayName);

    void SetQuality(int quality);
}