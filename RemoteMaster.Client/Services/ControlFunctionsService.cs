using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Services;

public class ControlFunctionsService
{
    public HubConnection ServerConnection { get; set; }

    public IEnumerable<DisplayInfo> Displays { get; set; }
}
