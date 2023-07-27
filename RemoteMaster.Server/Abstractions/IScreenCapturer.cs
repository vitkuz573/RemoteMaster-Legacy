using System.Drawing;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer : IDisposable
{
    int Quality { get; set; }

    event EventHandler<Rectangle> ScreenChanged;

    Rectangle CurrentScreenBounds { get; }

    Rectangle VirtualScreenBounds { get; }

    string SelectedScreen { get; }

    byte[]? GetNextFrame();

    IEnumerable<DisplayInfo> GetDisplays();

    void SetSelectedScreen(string displayName);
}