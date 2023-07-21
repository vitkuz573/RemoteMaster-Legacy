using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;
using System.Web;

namespace RemoteMaster.Client.Pages;

public partial class Control : IDisposable
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private NavigationManager NavManager { get; set; }

    [Inject]
    private ControlFunctionsService ControlFuncsService { get; set; }

    [Inject]
    private IHubConnectionFactory HubConnectionFactory { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string? _screenDataUrl;
    private HubConnection? _agentConnection;
    private HubConnection? _serverConnection;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoidAsync("setTitle", Host);

        if (firstRender)
        {
            ControlFuncsService.KillServer = async () =>
            {
                if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
                {
                    await _serverConnection.InvokeAsync("KillServer");
                }
            };

            ControlFuncsService.RebootComputer = async () =>
            {
                if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
                {
                    await _serverConnection.InvokeAsync("RebootComputer");
                }
            };

            ControlFuncsService.SetQuality = async (quality) =>
            {
                if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
                {
                    await _serverConnection.InvokeAsync("SetQuality", quality);
                }
            };

            var uri = new Uri(NavManager.Uri);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var skipAgentConnection = query.Get("skipAgent");

            if (skipAgentConnection != "true")
            {
                _agentConnection = HubConnectionFactory.Create(Host, 3564, "hubs/main");

                await _agentConnection.StartAsync();
                await _agentConnection.StopAsync();
            }

            _serverConnection = HubConnectionFactory.Create(Host, 5076, "hubs/control", withMessagePack: true);

            _serverConnection.On<ScreenDataDto>("ScreenData", dto =>
            {
                ControlFuncsService.Displays = dto.Displays;
                ControlFuncsService.SelectDisplay = async (display) =>
                {
                    if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
                    {
                        await _serverConnection.InvokeAsync("SendSelectedScreen", display);
                    }
                };
            });

            _serverConnection.On<ScreenSizeDto>("ScreenSize", dto =>
            {
                // logic
                // Properties: Width and Height
                // Invoked on change screen
            });

            _serverConnection.On<ChunkDto>("ScreenUpdate", async chunk =>
            {
                if (Chunker.TryUnchunkify(chunk, out var allData))
                {
                    _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);

                    await InvokeAsync(StateHasChanged);

                    await JSRuntime.InvokeVoidAsync("disableContextMenuOnImage");
                }
            });

            await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
            await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));

            await _serverConnection.StartAsync();
        }
    }

    private async Task<(double, double)> GetRelativeMousePositionOnPercent(MouseEventArgs e)
    {
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DOMRect>("getBoundingClientRect");

        var percentX = (e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (e.ClientY - imgPosition.Top) / imgPosition.Height;

        return (percentX, percentY);
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        var xyPercent = await GetRelativeMousePositionOnPercent(e);

        var dto = new MouseMoveDto
        {
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendMouseCoordinates", dto);
        }
    }

    private async Task OnMouseUpDown(MouseEventArgs e)
    {
        await SendMouseButton(e, e.Type == "mouseup" ? ButtonAction.Up : ButtonAction.Down);
    }

    private async Task OnMouseOver(MouseEventArgs e)
    {
        await SendMouseButton(e, ButtonAction.Up);
    }

    private async Task SendMouseButton(MouseEventArgs e, ButtonAction state)
    {
        var xyPercent = await GetRelativeMousePositionOnPercent(e);

        var dto = new MouseButtonClickDto
        {
            Button = e.Button,
            State = state,
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendMouseButton", dto);
        }
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        var dto = new MouseWheelDto
        {
            DeltaY = (int)e.DeltaY
        };

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendMouseWheel", dto);
        }
    }

    [JSInvokable]
    public async Task OnKeyDown(int keyCode)
    {
        await SendKeyboardInput(keyCode, ButtonAction.Down);
    }

    [JSInvokable]
    public async Task OnKeyUp(int keyCode)
    {
        await SendKeyboardInput(keyCode, ButtonAction.Up);
    }

    private async Task SendKeyboardInput(int keyCode, ButtonAction state)
    {
        var dto = new KeyboardKeyDto
        {
            Key = keyCode,
            State = state,
        };

        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync("SendKeyboardInput", dto);
        }
    }

    public void Dispose()
    {
        _serverConnection?.DisposeAsync();
        _agentConnection?.DisposeAsync();
    }
}