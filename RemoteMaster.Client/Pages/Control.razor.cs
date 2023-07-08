using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Dto;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private ILogger<Control> Logger { get; set; }

    [Inject]
    private IHubConnectionBuilder HubConnectionBuilder { get; set; }

    private HubConnection _hubConnection;
    private string? _statusMessage;
    private string? _screenDataUrl;

    private List<byte> _buffer = new List<byte>();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _hubConnection = HubConnectionBuilder
                .WithUrl($"http://{Host}:5076/hubs/control")
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            _hubConnection.On<ScreenUpdateDto>("ScreenUpdate", (dto) =>
            {
                if (dto.IsEndOfImage)
                {
                    var fullImageData = _buffer.ToArray();
                    _screenDataUrl = $"data:image/png;base64,{Convert.ToBase64String(fullImageData)}";
                    _statusMessage = null;
                    InvokeAsync(StateHasChanged);
                    _buffer.Clear();
                }
                else
                {
                    _buffer.AddRange(dto.Data);
                }
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
