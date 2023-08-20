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
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using RemoteMaster.Shared.Models;
using TypedSignalR.Client;

namespace RemoteMaster.Client.Pages;

public partial class Control : IAsyncDisposable
{
#nullable disable
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private ControlFunctionsService ControlFunctionsService { get; set; }

    [Inject]
    private IConnectionManager ConnectionManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private IQueryParameterService QueryParameterService { get; set; }
#nullable restore

    private TaskCompletionSource<bool> _agentHandledTcs = new();
    private string _statusMessage = "Establishing connection...";
    private string? _screenDataUrl;
    private IControlHub _controlHubProxy;
    private bool _serverTampered = false;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (!QueryParameterService.GetValueFromQuery<bool>("skipAgent"))
            {
                await InitializeAgentConnectionAsync();
            }

            if (!_serverTampered)
            {
                await InitializeServerConnectionAsync();
                await _controlHubProxy.ConnectAs(Intention.Control);
            }

            await SetupClientEventListeners();
        }
    }

    private void HandleScreenData(ScreenDataDto dto)
    {
        ControlFunctionsService.Displays = dto.Displays;
    }

    private async Task HandleServerTampered(string message)
    {
        _statusMessage = message;
        _serverTampered = true;
        await InvokeAsync(StateHasChanged);
        await ConnectionManager.DisconnectAsync("Agent");
        _agentHandledTcs.SetResult(true);
    }

    private async Task HandleScreenUpdate(ChunkWrapper chunk)
    {
        if (Chunker.TryUnchunkify(chunk, out var allData))
        {
            _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", allData);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task SetupClientEventListeners()
    {
        await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
    }

    private async Task InitializeAgentConnectionAsync()
    {
        await ConnectionManager
            .Connect("Agent", $"http://{Host}:3564/hubs/main")
            .On<string>("ServerTampered", HandleServerTampered)
            .StartAsync();
        
        await WaitForAgentOrTimeoutAsync();
        await _agentHandledTcs.Task;
    }

    private async Task InitializeServerConnectionAsync()
    {
        var serverContext = await ConnectionManager
            .Connect("Server", $"http://{Host}:5076/hubs/control", true)
            .On<ScreenDataDto>("ReceiveScreenData", HandleScreenData)
            .On<ChunkWrapper>("ReceiveScreenUpdate", HandleScreenUpdate)
            .StartAsync();

        _controlHubProxy = serverContext.Connection.CreateHubProxy<IControlHub>();
        ControlFunctionsService.ControlHubProxy = _controlHubProxy;
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

        await _controlHubProxy.SendMouseCoordinates(new MouseMoveDto
        {
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        });
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

        await _controlHubProxy.SendMouseButton(new MouseClickDto
        {
            Button = e.Button,
            State = state,
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        });
    }

    private async Task OnMouseWheel(WheelEventArgs e)
    {
        await _controlHubProxy.SendMouseWheel(new MouseWheelDto
        {
            DeltaY = (int)e.DeltaY
        });
    }

    private async Task SendKeyboardInput(int keyCode, ButtonAction state)
    {
        await _controlHubProxy.SendKeyboardInput(new KeyboardKeyDto
        {
            Key = keyCode,
            State = state
        });
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

    public async ValueTask DisposeAsync()
    {
        await ConnectionManager.DisconnectAsync("Server");
        await ConnectionManager.DisconnectAsync("Agent");
    }
}