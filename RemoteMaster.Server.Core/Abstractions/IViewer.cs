using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Core.Abstractions;

public interface IViewer
{
    IScreenCapturer ScreenCapturer { get; }

    string ConnectionId { get; }

    Task StartStreaming(CancellationToken cancellationToken);

    Task SendScreenData(IEnumerable<DisplayInfo> displays, int screenWidth, int screenHeight);

    Task SendScreenSize(int width, int height);

    void SetSelectedScreen(string displayName);
}
