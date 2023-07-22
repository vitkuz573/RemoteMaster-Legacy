using Windows.Win32.UI.WindowsAndMessaging;

namespace RemoteMaster.Shared.Dtos;

public class MessageBoxDto
{
    public string Caption { get; init; }

    public string Text { get; init; }

    public MESSAGEBOX_STYLE Style { get; init; }
}
