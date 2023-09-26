// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using TypedSignalR.Client;

namespace RemoteMaster.Server.Pages;

public partial class Connect : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private IConnectionManager ConnectionManager { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    private string _statusMessage = "Establishing connection...";
    private string? _screenDataUrl;

    private bool _isMenuOpen = false;

    private string _clientVersion;
    private string _agentVersion;
    
    private bool _inputEnabled;
    private bool _cursorTracking;
    private int _imageQuality;

    private string _powerMessage = "test";
    private int _timeout = 60;

    private IEnumerable<DisplayInfo> _displays;

    private IControlHub _controlHubProxy;
    private HubConnection _agentConnection;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await GetVersions();

            await InitializeClientConnectionAsync();
            await InitializeAgentConnectionAsync();
            await _controlHubProxy.ConnectAs(Intention.Control);

            await SetupClientEventListeners();

            var uri = new Uri(NavigationManager.Uri);
            var queryParameters = QueryHelpers.ParseQuery(uri.Query);

            if (queryParameters.TryGetValue("intention", out var intentionValue))
            {
                if (Enum.TryParse(typeof(Intention), intentionValue, true, out var parsedIntention))
                {
                    var intention = (Intention)parsedIntention;
                }
            }
        }
    }

    private void HandleClientConfiguration(ClientConfigurationDto dto)
    {
        _inputEnabled = dto.InputEnabled;
        _cursorTracking = dto.TrackCursor;
        _imageQuality = dto.ImageQuality;
    }

    private void HandleScreenData(ScreenDataDto dto)
    {
        _displays = dto.Displays;
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

    }

    private async Task InitializeAgentConnectionAsync()
    {
        var agentContext = await ConnectionManager
            .Connect("Agent", $"http://{Host}:3564/hubs/maintenance")
            .StartAsync();

        _agentConnection = agentContext.Connection;
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
        await ConnectionManager.DisconnectAsync("Agent");
    }

    private async Task ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private async void OnChangeScreen(ChangeEventArgs e)
    {
        await _controlHubProxy.SendSelectedScreen(e.Value.ToString());
    }

    private async void ChangeQuality(int quality)
    {
        _imageQuality = quality;

        await _controlHubProxy.SetQuality(quality);
    }

    private async Task ToggleCursorTracking(bool value)
    {
        _cursorTracking = value;

        await _controlHubProxy.SetTrackCursor(value);
    }

    private async Task ToggleInputEnabled(bool value)
    {
        _inputEnabled = value;

        await _controlHubProxy.SetInputEnabled(value);
    }

    private async void KillClient()
    {
        await _controlHubProxy.KillClient();
    }

    private async void RebootComputer()
    {
        await _controlHubProxy.RebootComputer(_powerMessage, _timeout, true);
    }

    private async void ShutdownComputer()
    {
        await _controlHubProxy.ShutdownComputer(_powerMessage, _timeout, true);
    }

    private async void SendCtrlAltDel()
    {
        await _agentConnection.InvokeAsync("SendCtrlAltDel");
    }

    private async Task GetVersions()
    {
        var url = $"http://{Host}:5124/api/versions";

        using var client = new HttpClient();

        try
        {
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(result))
                {
                    return;
                }

                var versions = JsonSerializer.Deserialize<List<VersionInfo>>(result);

                if (versions == null || versions.Count == 0)
                {
                    return;
                }

                foreach (var version in versions)
                {
                    if (version.ComponentName.Equals("Agent", StringComparison.OrdinalIgnoreCase))
                    {
                        _agentVersion = version.CurrentVersion;
                    }
                    else if (version.ComponentName.Equals("Client", StringComparison.OrdinalIgnoreCase))
                    {
                        _clientVersion = version.CurrentVersion;
                    }
                }
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}