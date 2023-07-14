using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Client.Models;
using RemoteMaster.Shared.Dto;
using RemoteMaster.Shared.Helpers;
using System.Web;

namespace RemoteMaster.Client.Pages;

public partial class Control
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private NavigationManager NavManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string? _screenDataUrl;
    private HubConnection? _agentConnection;
    private HubConnection? _serverConnection;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var uri = new Uri(NavManager.Uri);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var skipAgentConnection = query.Get("skipAgent");

            if (skipAgentConnection != "true")
            {
                _agentConnection = new HubConnectionBuilder()
                    .WithUrl($"http://{Host}:3564/hubs/main", options =>
                    {
                        options.SkipNegotiation = true;
                        options.Transports = HttpTransportType.WebSockets;
                    })
                    .Build();

                await _agentConnection.StartAsync();

                Thread.Sleep(5000);

                await _agentConnection.StopAsync();
            }

            _serverConnection = new HubConnectionBuilder()
                .WithUrl($"http://{Host}:5076/hubs/control", options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();

            _serverConnection.On<ChunkDto>("ScreenUpdate", async chunk =>
            {
                if (Chunker.TryUnchunkify<byte[]>(chunk, out var allData))
                {
                    _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);

                    await InvokeAsync(StateHasChanged);
                }
            });

            await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
            await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));

            await _serverConnection.StartAsync();
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

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendMouseCoordinates", dto);
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

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendMouseButton", dto);
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

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendMouseButton", dto);
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

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendKeyboardInput", dto);
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


        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendKeyboardInput", dto);
        }
    }
}

