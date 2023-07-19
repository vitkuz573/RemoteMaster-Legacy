namespace RemoteMaster.Server.Abstractions;

public interface IScreenCapturer
{
    byte[]? GetNextFrame();

    IEnumerable<string> GetDisplayNames();

    void SetSelectedScreen(string displayName);
}