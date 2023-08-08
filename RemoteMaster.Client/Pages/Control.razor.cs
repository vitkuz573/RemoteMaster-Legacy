using System.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Client.Abstractions;
using RemoteMaster.Client.Models;
using RemoteMaster.Client.Services;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Pages;

public partial class Control : IAsyncDisposable
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

    [Inject]
    private ILogger<Control> Logger { get; set; }

    private TaskCompletionSource<bool> _agentHandledTcs = new();
    private string _notificationMessage = "Establishing connection...";
    private string? _screenDataUrl;
    private HubConnection? _agentConnection;
    private HubConnection? _serverConnection;
    private bool _serverTampered = false;

    private static bool IsConnectionReady(HubConnection connection) => connection != null && connection.State == HubConnectionState.Connected;

    private async Task TryInvokeServerAsync<T>(string method, T argument)
    {
        if (IsConnectionReady(_serverConnection))
        {
            await _serverConnection.InvokeAsync(method, argument);
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            ParseUriAndCheckParameters();

            await InitializeAgentConnection();

            InitializeServerConnection();

            await SetupClientEventListeners();

            await HandleAgentConnectionStatus();
        }
    }

    private void ParseUriAndCheckParameters()
    {
        var uriCreated = Uri.TryCreate(NavManager.Uri, UriKind.Absolute, out var uri);

        if (!uriCreated || uri == null)
        {
            throw new UriFormatException($"Could not parse the URI: {NavManager.Uri}");
        }

        var query = HttpUtility.ParseQueryString(uri.Query);
        var skipAgentConnection = query.Get("skipAgent");

        if (skipAgentConnection == "true")
        {
            // Handle or set a flag if needed
        }
    }

    private async Task InitializeAgentConnection()
    {
        _agentConnection = HubConnectionFactory.Create(Host, 3564, "hubs/main");

        _agentConnection.On<string>("ServerTampered", async message =>
        {
            Logger.LogInformation("Received ServerTampered message: {Message}", message);
            _notificationMessage = message;
            _serverTampered = true;
            await InvokeAsync(StateHasChanged);
            await _agentConnection.StopAsync();

            _agentHandledTcs.SetResult(true);
        });

        await _agentConnection.StartAsync();
    }

    private void InitializeServerConnection()
    {
        _serverConnection = HubConnectionFactory.Create(Host, 5076, "hubs/control", withMessagePack: true);

        _serverConnection.On<ScreenDataDto>("ScreenData", dto => ControlFuncsService.Displays = dto.Displays);

        _serverConnection.On<ChunkWrapper>("ScreenUpdate", async chunk =>
        {
            if (Chunker.TryUnchunkify(chunk, out var allData))
            {
                _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);
                await InvokeAsync(StateHasChanged);
                await JSRuntime.InvokeVoidAsync("disableContextMenuOnImage");
            }
        });
    }

    private async Task SetupClientEventListeners()
    {
        await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
    }

    private async Task HandleAgentConnectionStatus()
    {
        await WaitForAgentOrTimeoutAsync();

        await _agentHandledTcs.Task;

        if (!_serverTampered)
        {
            Logger.LogInformation("Attempting to start _serverConnection");
            await _serverConnection.StartAsync();
            ControlFuncsService.ServerConnection = _serverConnection;
        }
        else
        {
            Logger.LogInformation("_serverTampered is true, not starting _serverConnection");
        }
    }

    private async Task WaitForAgentOrTimeoutAsync(int timeoutMilliseconds = 5000)
    {
        var timeoutTask = Task.Delay(timeoutMilliseconds);
        var completedTask = await Task.WhenAny(_agentHandledTcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Logger.LogWarning("Timeout while waiting for agent response.");
            _agentHandledTcs.TrySetResult(false);
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

        await TryInvokeServerAsync("SendMouseCoordinates", dto);
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

        var dto = new MouseClickDto
        {
            Button = e.Button,
            State = state,
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        await TryInvokeServerAsync("SendMouseButton", dto);
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        var dto = new MouseWheelDto
        {
            DeltaY = (int)e.DeltaY
        };

        await TryInvokeServerAsync("SendMouseWheel", dto);
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

        await TryInvokeServerAsync("SendKeyboardInput", dto);
    }

    public async ValueTask DisposeAsync()
    {
        if (_serverConnection != null)
        {
            await _serverConnection.DisposeAsync();
        }

        if (_agentConnection != null)
        {
            await _agentConnection.DisposeAsync();
        }
    }
}
