using SkiaSharp;

namespace RemoteMaster.Server.Abstractions;

public interface IViewerService
{
    SKEncodedImageFormat GetImageFormat();

    int GetImageQuality();

    void SetImageQuality(int quality);
}