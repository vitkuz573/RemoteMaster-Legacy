using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    private HubConnection? _hubConnection;
    private string? _statusMessage;
    private string? _screenDataUrl;

    [Inject]
    private ILogger<Control> Logger { get; set; }

    private bool _firstRender = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_firstRender && Host != null && _hubConnection == null)
        {
            _firstRender = false;
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{Host}:5076/controlHub")
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

            Logger.LogInformation("Connection started!");
        }
    }

    public async Task DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
