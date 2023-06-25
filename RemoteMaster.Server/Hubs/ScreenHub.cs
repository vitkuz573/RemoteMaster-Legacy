using Microsoft.AspNetCore.SignalR;
using System.Text;

namespace RemoteMaster.Server.Hubs;

public class ScreenHub : Hub
{
    public async Task SendScreenUpdate(string ipAddress, byte[] screenData)
    {
        await Clients.OthersInGroup(ipAddress).SendAsync("ScreenUpdate", screenData);
        // temporary code for testing
        if (screenData == null)
        {
            await Clients.Group(ipAddress).SendAsync("ScreenUpdate", Encoding.UTF8.GetBytes("Hello, world!"));
        }
    }

    public async Task ShowDialog(string message)
    {
        // Реализация отображения диалога на сервере
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext.Request.Query["ipAddress"];
        Console.WriteLine($"Client with IP {ipAddress} connected.");
        await Groups.AddToGroupAsync(Context.ConnectionId, ipAddress);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext.Request.Query["ipAddress"];
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ipAddress);
        await base.OnDisconnectedAsync(exception);
    }
}
