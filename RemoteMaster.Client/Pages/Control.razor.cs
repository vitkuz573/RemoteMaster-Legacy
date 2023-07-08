using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private ILogger<Control> Logger { get; set; }

    private HubConnection _hubConnection;
    private string? _statusMessage;
    private string? _screenDataUrl;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{Host}:5076/hubs/control")
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            _hubConnection.On<byte[]>("ScreenUpdate", (screenData) =>
            {
                _screenDataUrl = $"data:image/png;base64,{Convert.ToBase64String(screenData)}";
                _statusMessage = null;
                InvokeAsync(StateHasChanged);
            });

            await _hubConnection.StartAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
