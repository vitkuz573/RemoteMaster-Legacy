using System.Drawing;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public HubConnection ServerConnection { get; set; }

    public IEnumerable<(string, bool, Size)> Displays { get; set; }
}
