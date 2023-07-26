using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Dtos;
using System.Drawing;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public HubConnection ServerConnection { get; set; }

    public IEnumerable<(string, bool, Size)> Displays { get; set; }

    public Action<string> SelectDisplay { get; set; }

    public Action<int> SetQuality { get; set; }

    public Action<MessageBoxDto> SendMessageBox { get; set; }
}
