    using Microsoft.Extensions.Options;
    using RemoteMaster.Server.Abstractions;
    using RemoteMaster.Shared.Options;
    using SkiaSharp;

    namespace RemoteMaster.Server.Services;

    public class ViewerService : IViewerService
    {
        private readonly IOptionsSnapshot<ViewerOptions> _options;

        public ViewerService(IOptionsSnapshot<ViewerOptions> options)
        {
            _options = options;
        }

        public SKEncodedImageFormat GetImageFormat()
        {
            return _options.Value.ImageFormat switch
            {
                "Png" => SKEncodedImageFormat.Png,
                "Jpeg" => SKEncodedImageFormat.Jpeg,
                _ => SKEncodedImageFormat.Jpeg,
            };
        }

        public int GetImageQuality()
        {
            return _options.Value.ImageQuality ?? 80;
        }

        public void SetImageQuality(int quality)
        {
            if (quality < 1 || quality > 100)
            {
                throw new ArgumentException("Quality must be between 1 and 100.");
            }

            _options.Value.ImageQuality = quality;
        }
    }