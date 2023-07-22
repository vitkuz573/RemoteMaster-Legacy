using RemoteMaster.Shared.Dtos;
using System.Drawing;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public Action KillServer { get; set; }

    public Action RebootComputer { get; set; }

    public IEnumerable<(string, bool, Size)> Displays { get; set; }

    public Action<string> SelectDisplay { get; set; }

    public Action<int> SetQuality { get; set; }

    public Action<MessageBoxDto> SendMessageBox { get; set; }
}
