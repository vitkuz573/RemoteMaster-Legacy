using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Client.Models;
using RemoteMaster.Shared.Dto;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private IHubConnectionBuilder HubConnectionBuilder { get; set; }

    private string? _screenDataUrl;
    private HubConnection? _hubConnection;
    private readonly List<byte[]> _buffer = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _hubConnection = HubConnectionBuilder
                .WithUrl($"http://{Host}:5076/hubs/control", options => {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            _hubConnection.On<ScreenUpdateDto>("ScreenUpdate", async dto =>
            {
                _buffer.Add(dto.Data);

                if (dto.IsEndOfImage)
                {
                    var allData = _buffer.SelectMany(bytes => bytes).ToArray();
                    _buffer.Clear();

                    _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);

                    await InvokeAsync(StateHasChanged);
                }
            });

            await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
            await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));

            await _hubConnection.StartAsync();
        }
    }

    public async Task QualityChanged(ChangeEventArgs e)
    {
        var quality = int.Parse(e.Value.ToString());

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SetQuality", quality);
        }
    }

    private async Task<(int, int)> GetNormalizedMouseCoordinates(MouseEventArgs e)
    {
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DOMRect>("getBoundingClientRect");

        var relativeX = e.ClientX - imgPosition.Left;
        var relativeY = e.ClientY - imgPosition.Top;

        var absoluteX = (int)Math.Round(relativeX * 65535 / imgPosition.Width);
        var absoluteY = (int)Math.Round(relativeY * 65535 / imgPosition.Height);

        return (absoluteX, absoluteY);
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        var (absoluteX, absoluteY) = await GetNormalizedMouseCoordinates(e);

        var dto = new MouseMoveDto
        {
            X = absoluteX,
            Y = absoluteY
        };

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMouseCoordinates", dto);
        }
    }

    private async Task OnMouseUpDown(MouseEventArgs e)
    {
        var (absoluteX, absoluteY) = await GetNormalizedMouseCoordinates(e);

        var dto = new MouseButtonClickDto
        {
            Button = e.Button,
            State = e.Type,
            X = absoluteX,
            Y = absoluteY
        };

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMouseButton", dto);
        }
    }

    private async Task OnMouseOver(MouseEventArgs e)
    {
        var (absoluteX, absoluteY) = await GetNormalizedMouseCoordinates(e);

        var dto = new MouseButtonClickDto
        {
            Button = e.Button,
            State = "mouseup",
            X = absoluteX,
            Y = absoluteY
        };

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendMouseButton", dto);
        }
    }

    [JSInvokable]
    public async Task OnKeyDown(int keyCode)
    {
        var dto = new KeyboardKeyDto
        {
            Key = keyCode,
            State = "keydown",
        };

        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendKeyboardInput", dto);
        }
    }

    [JSInvokable]
    public async Task OnKeyUp(int keyCode)
    {
        var dto = new KeyboardKeyDto
        {
            Key = keyCode,
            State = "keyup",
        };


        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("SendKeyboardInput", dto);
        }
    }
}

