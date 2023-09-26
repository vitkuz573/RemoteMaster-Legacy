// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Services;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using TypedSignalR.Client;

namespace RemoteMaster.Server.Pages;

public partial class Connect : IAsyncDisposable
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
#nullable restore

    private string _statusMessage = "Establishing connection...";
    private string? _screenDataUrl;
    private IControlHub _controlHubProxy;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeClientConnectionAsync();
            await InitializeAgentConnectionAsync();
            await _controlHubProxy.ConnectAs(Intention.Control);

            await SetupClientEventListeners();

            // await JSRuntime.InvokeVoidAsync("startClipboardMonitoring", DotNetObjectReference.Create(this));
        }
    }

    private void HandleClientConfiguration(ClientConfigurationDto dto)
    {
        ControlFunctionsService.ClientConfiguration = dto;
    }

    private void HandleScreenData(ScreenDataDto dto)
    {
        ControlFunctionsService.Displays = dto.Displays;
    }

    private async Task HandleScreenUpdate(byte[] screenData)
    {
        _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", screenData);
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetupClientEventListeners()
    {
        await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
    }

    private async Task InitializeClientConnectionAsync()
    {
        var clientContext = await ConnectionManager
            .Connect("Client", $"http://{Host}:5076/hubs/control", true)
            .On<ClientConfigurationDto>("ReceiveClientConfiguration", HandleClientConfiguration)
            .On<ScreenDataDto>("ReceiveScreenData", HandleScreenData)
            .On<byte[]>("ReceiveScreenUpdate", HandleScreenUpdate)
            .StartAsync();

        _controlHubProxy = clientContext.Connection.CreateHubProxy<IControlHub>();
        ControlFunctionsService.ControlHubProxy = _controlHubProxy;
        ControlFunctionsService.Host = Host;
    }

    private async Task InitializeAgentConnectionAsync()
    {
        var agentContext = await ConnectionManager
            .Connect("Agent", $"http://{Host}:3564/hubs/maintenance")
            .StartAsync();

        ControlFunctionsService.AgentConnection = agentContext.Connection;
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
        await ConnectionManager.DisconnectAsync("Client");
    }
}