// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

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
#nullable disable
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private ControlFunctionsService ControlFunctionsService { get; set; }

    [Inject]
    private IHubConnectionFactory HubConnectionFactory { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private IUriParametersService UriParametersService { get; set; }
#nullable restore

    private UriParameters UriParameters
    {
        get
        {
            _uriParameters ??= new UriParameters
            {
                SkipAgent = UriParametersService.GetParameter<bool>("skipAgent")
            };

            return _uriParameters;
        }
    }

    private UriParameters _uriParameters;
    private TaskCompletionSource<bool> _agentHandledTcs = new();
    private string _statusMessage = "Establishing connection...";
    private string? _screenDataUrl;
    private HubConnection? _agentConnection;
    private HubConnection? _serverConnection;
    private bool _serverTampered = false;

    private async Task TryInvokeServerAsync<T>(string method, T argument)
    {
        if (_serverConnection != null && _serverConnection.State == HubConnectionState.Connected)
        {
            await _serverConnection.InvokeAsync(method, argument);
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            InitializeServerConnection();
            RegisterServerHandlers();
            await InitializeConnectionsAsync();
            await SetupClientEventListeners();
        }
    }

    private void InitializeAgentConnection()
    {
        _agentConnection = HubConnectionFactory.Create(Host, 3564, "hubs/main");
    }

    private void RegisterAgentHandlers()
    {
        _agentConnection.On<string>("ServerTampered", HandleServerTampered);
    }

    private void RegisterServerHandlers()
    {
        _serverConnection.On<ScreenDataDto>("ScreenData", HandleScreenData);
        _serverConnection.On<ChunkWrapper>("ScreenUpdate", HandleScreenUpdate);
    }

    private void HandleScreenData(ScreenDataDto dto)
    {
        ControlFunctionsService.Displays = dto.Displays;
    }

    private async Task HandleServerTampered(string message)
    {
        _statusMessage = message;
        _serverTampered = true;
        await RefreshUI();
        await _agentConnection.StopAsync();
        _agentHandledTcs.SetResult(true);
    }

    private void InitializeServerConnection()
    {
        _serverConnection = HubConnectionFactory.Create(Host, 5076, "hubs/control", withMessagePack: true);
    }

    private async Task HandleScreenUpdate(ChunkWrapper chunk)
    {
        if (Chunker.TryUnchunkify(chunk, out var allData))
        {
            _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);
            await RefreshUI();
        }
    }

    private async Task SetupClientEventListeners()
    {
        await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
    }

    private async Task InitializeConnectionsAsync()
    {
        if (!UriParameters.SkipAgent)
        {
            InitializeAgentConnection();
            RegisterAgentHandlers();
            await _agentConnection.StartAsync();
            await HandleAgentConnectionStatus();
        }
        else
        {
            await StartServerConnectionAsync();
        }
    }

    private async Task StartServerConnectionAsync()
    {
        if (_serverConnection != null)
        {
            await _serverConnection.StartAsync();
            ControlFunctionsService.ServerConnection = _serverConnection;
            await _serverConnection.InvokeAsync("ConnectAs", Intention.Control);
        }
    }

    private async Task HandleAgentConnectionStatus()
    {
        await WaitForAgentOrTimeoutAsync();
        await _agentHandledTcs.Task;

        if (!_serverTampered)
        {
            await StartServerConnectionAsync();
        }
    }

    private async Task WaitForAgentOrTimeoutAsync(int timeoutMilliseconds = 5000)
    {
        var timeoutTask = Task.Delay(timeoutMilliseconds);
        var completedTask = await Task.WhenAny(_agentHandledTcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _agentHandledTcs.TrySetResult(false);
        }
    }

    private async Task<(double, double)> GetRelativeMousePositionPercentAsync(MouseEventArgs e)
    {
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DOMRect>("getBoundingClientRect");
        var percentX = (e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (e.ClientY - imgPosition.Top) / imgPosition.Height;

        return (percentX, percentY);
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        var xyPercent = await GetRelativeMousePositionPercentAsync(e);
        var dto = new MouseMoveDto
        {
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        await TryInvokeServerAsync("SendMouseCoordinates", dto);
    }

    private async Task OnMouseUpDown(MouseEventArgs e)
    {
        var state = e.Type == "mouseup" ? ButtonAction.Up : ButtonAction.Down;
        await SendMouseInputAsync(e, state);
    }

    private async Task OnMouseOver(MouseEventArgs e)
    {
        await SendMouseInputAsync(e, ButtonAction.Up);
    }

    private async Task SendMouseInputAsync(MouseEventArgs e, ButtonAction state)
    {
        var xyPercent = await GetRelativeMousePositionPercentAsync(e);
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

    private async Task SendKeyboardInput(int keyCode, ButtonAction state)
    {
        var dto = new KeyboardKeyDto
        {
            Key = keyCode,
            State = state
        };

        await TryInvokeServerAsync("SendKeyboardInput", dto);
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

    private async Task RefreshUI()
    {
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        await _serverConnection.DisposeAsync();

        if (_agentConnection != null)
        {
            await _agentConnection.DisposeAsync();
        }
    }
}
