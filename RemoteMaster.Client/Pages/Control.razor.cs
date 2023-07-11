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
                .Build();

            _hubConnection.On<ScreenUpdateDto>("ScreenUpdate", async dto =>
            {
                _buffer.Add(dto.Data);

                if (dto.IsEndOfImage)
                {
                    var allData = _buffer.SelectMany(bytes => bytes).ToArray();
                    _buffer.Clear();

                    var url = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);

                    await UpdateScreenDataUrl(url);
                }
            });

            await _hubConnection.StartAsync();
        }
    }

    public async Task UpdateScreenDataUrl(string url)
    {
        _screenDataUrl = url;
        await InvokeAsync(StateHasChanged);
    }

    public async Task QualityChanged(ChangeEventArgs e)
    {
        var quality = int.Parse(e.Value.ToString());

        await _hubConnection.InvokeAsync("SetQuality", quality);
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
        await _hubConnection.InvokeAsync("SendMouseCoordinates", absoluteX, absoluteY);
    }

    private async Task OnMouseUpDown(MouseEventArgs e)
    {
        var (absoluteX, absoluteY) = await GetNormalizedMouseCoordinates(e);
        await _hubConnection.InvokeAsync("SendMouseButton", e.Button, e.Type, absoluteX, absoluteY);
    }
}

