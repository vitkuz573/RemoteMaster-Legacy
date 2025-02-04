using RemoteMaster.Host.Linux.Abstractions;
using Tmds.DBus;

namespace RemoteMaster.Host.Linux.Services
{
    /// <summary>
    /// A screen capturing service implemented via xdg‑desktop‑portal.
    /// When GetNextFrame is called, it sends a request to the portal,
    /// reads the screenshot file from disk, and returns its contents as a byte array.
    /// </summary>
    public class XdgPortalScreenCapturingService : ScreenCapturingService
    {
        private readonly Connection _dbus;
        private readonly IScreenshotPortal _portal;

        public XdgPortalScreenCapturingService()
        {
            // Connect to the session DBus.
            _dbus = new Connection(Address.Session);
            _dbus.ConnectAsync().GetAwaiter().GetResult();

            // Create a proxy for the screenshot portal using the standard object path.
            _portal = _dbus.CreateProxy<IScreenshotPortal>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
        }

        /// <summary>
        /// Requests a screenshot via xdg‑desktop‑portal and returns the image data.
        /// </summary>
        /// <param name="connectionId">A connection identifier (not used in this implementation).</param>
        /// <returns>The screenshot as a byte array, or null if an error occurred.</returns>
        public override byte[]? GetNextFrame(string connectionId)
        {
            var options = new Dictionary<string, object>
            {
                // Set interactive=false to obtain a screenshot without prompting the user,
                // if the portal's security policy allows.
                { "interactive", false }
            };

            // Pass an empty string as the parent window handle.
            (uint response, string? uri) result = _portal.ScreenshotAsync("", options)
                .GetAwaiter()
                .GetResult();

            if (result.response != 0 || string.IsNullOrEmpty(result.uri) || !result.uri.StartsWith("file://"))
            {
                Console.WriteLine("Error: Received an invalid response from the screenshot portal.");
                return null;
            }

            // Remove the "file://" prefix to obtain the local file path.
            var filePath = result.uri.Replace("file://", "");
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading screenshot file: {ex.Message}");
                return null;
            }
        }

        public override void Dispose()
        {
            _dbus.Dispose();
        }
    }
}
