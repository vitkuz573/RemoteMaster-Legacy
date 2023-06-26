using Microsoft.AspNetCore.SignalR;
using ScreenHelper;
using System.Drawing;
using System.Drawing.Imaging;

namespace RemoteMaster.Server.Hubs;

public class ScreenHub : Hub
{
    private readonly ILogger<ScreenHub> _logger; // Поле для экземпляра логгера

    public ScreenHub(ILogger<ScreenHub> logger) // Внедряем логгер через конструктор
    {
        _logger = logger;
    }

    public byte[] CaptureScreen()
    {
        _logger.LogInformation("Capturing screen..."); // Добавляем логирование перед захватом экрана

        using var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        }

        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);

        _logger.LogInformation($"Captured screen of size {memoryStream.Length} bytes"); // Добавляем логирование после захвата экрана

        return memoryStream.ToArray();
    }

    public async Task SendScreenUpdate(string ipAddress)
    {
        _logger.LogInformation($"Sending screen update for IP {ipAddress}"); // Добавляем логирование перед отправкой обновления экрана

        var screenData = CaptureScreen();
        await Clients.OthersInGroup(ipAddress).SendAsync("ScreenUpdate", screenData);
    }

    public async Task ShowDialog(string message)
    {
        // Реализация отображения диалога на сервере
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext.Request.Query["ipAddress"];
        _logger.LogInformation($"Client with IP {ipAddress} connected."); // Добавляем логирование при подключении клиента
        await Groups.AddToGroupAsync(Context.ConnectionId, ipAddress);
        await base.OnConnectedAsync();

        await SendScreenUpdate(ipAddress); // вызываем SendScreenUpdate при подключении клиента
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var httpContext = Context.GetHttpContext();
        var ipAddress = httpContext.Request.Query["ipAddress"];
        _logger.LogInformation($"Client with IP {ipAddress} disconnected."); // Добавляем логирование при отключении клиента
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ipAddress);
        await base.OnDisconnectedAsync(exception);
    }
}
