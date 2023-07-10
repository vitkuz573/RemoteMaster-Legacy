using Microsoft.Extensions.Options;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Options;
using SkiaSharp;

namespace RemoteMaster.Server.Services;

public class ViewerService : IViewerService
{
    private readonly ViewerOptions _options;

    public ViewerService(IOptions<ViewerOptions> options)
    {
        _options = options.Value;
    }

    public SKEncodedImageFormat GetImageFormat()
    {
        return _options.ImageFormat switch
        {
            "Png" => SKEncodedImageFormat.Png,
            "Jpeg" => SKEncodedImageFormat.Jpeg,
            _ => SKEncodedImageFormat.Jpeg,
        };
    }

    public int GetImageQuality()
    {
        return _options.ImageQuality ?? 80;
    }
}
