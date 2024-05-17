// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Wrap;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    private string _transportType = string.Empty;
    private string? _screenDataUrl;
    private bool _drawerOpen;
    private HubConnection _connection = null!;
    private bool _inputEnabled;
    private bool _blockUserInput;
    private bool _cursorTracking;
    private int _imageQuality;
    private string _hostVersion = string.Empty;
    private List<Display> _displays = [];
    private string _selectedDisplay = string.Empty;
    private ElementReference _screenImageElement;
    private string _accessToken;
    private List<ViewerDto> _viewers = [];

    private readonly AsyncPolicyWrap _combinedPolicy;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthRight = "350px"
        }
    };

    private string? _title;
    private bool _firstRenderCompleted = false;

    public Access()
    {
        var retryPolicy = Policy
            .Handle<WebSocketException>()
            .Or<IOException>()
            .Or<SocketException>()
            .Or<InvalidOperationException>()
            .WaitAndRetryAsync(
            [
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(7),
                TimeSpan.FromSeconds(10)
            ]);

        var noRetryPolicy = Policy
            .Handle<HubException>(ex => ex.Message.Contains("Method does not exist"))
            .FallbackAsync(async (ct) =>
            {
                if (_firstRenderCompleted)
                {
                    await JsRuntime.InvokeVoidAsync("alert", "This function is not available in the current host version. Please update your host.");
                }
            });

        _combinedPolicy = Policy.WrapAsync(noRetryPolicy, retryPolicy);
    }

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(_title))
        {
            _title = Host;
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _firstRenderCompleted = true;
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/eventListeners.js");

            await module.InvokeVoidAsync("addPreventCtrlSListener");
            await module.InvokeVoidAsync("addBeforeUnloadListener", DotNetObjectReference.Create(this));
            await module.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
            await module.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));

            await InitializeHostConnectionAsync();
            await SetParametersFromUriAsync();
            await FetchViewersAsync();
        }
    }

    private async Task SetParametersFromUriAsync()
    {
        var uri = new Uri(NavigationManager.Uri);
        var newUri = uri.ToString();

        _imageQuality = QueryParameterService.GetParameter("imageQuality", 25);
        _cursorTracking = QueryParameterService.GetParameter("cursorTracking", false);
        _inputEnabled = QueryParameterService.GetParameter("inputEnabled", true);

        if (newUri != uri.ToString())
        {
            NavigationManager.NavigateTo(newUri, true);
        }

        await UpdateServerParameters();
    }

    private async Task UpdateServerParameters()
    {
        await _connection.InvokeAsync("SendImageQuality", _imageQuality);
        await _connection.InvokeAsync("SendToggleCursorTracking", _cursorTracking);
        await _connection.InvokeAsync("SendToggleInput", _inputEnabled);
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        await _combinedPolicy.ExecuteAsync(async () =>
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                await action();
            }
            else
            {
                throw new InvalidOperationException("Connection is not active");
            }
        });
    }

    private async Task KillHost()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKillHost"));
    }

    private async Task SendCtrlAltDel()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendCommandToService", "CtrlAltDel"));
    }

    private async Task RebootComputer()
    {
        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendRebootComputer", powerActionRequest));
    }

    private async Task ShutdownComputer()
    {
        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendShutdownComputer", powerActionRequest));
    }

    private async Task InitializeHostConnectionAsync()
    {
        _accessToken = await AccessTokenProvider.GetAccessTokenAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/control", options =>
            {
                options.AccessTokenProvider = async () => await Task.FromResult(_accessToken);
            })
            .AddMessagePackProtocol()
            .Build();

        _connection.On<IEnumerable<Display>>("ReceiveDisplays", (displays) =>
        {
            _displays = displays.ToList();

            var primaryDisplay = _displays.FirstOrDefault(d => d.IsPrimary);

            if (primaryDisplay != null)
            {
                _selectedDisplay = primaryDisplay.Name;
            }
        });

        _connection.On<byte[]>("ReceiveScreenUpdate", HandleScreenUpdate);
        _connection.On<Version>("ReceiveHostVersion", version => _hostVersion = version.ToString());
        _connection.On<string>("ReceiveTransportType", transportType => _transportType = transportType);
        _connection.On<List<ViewerDto>>("ReceiveAllViewers", viewers => _viewers = viewers);

        var connectRequest = new ConnectRequest(Intention.ManageDevice, "UserName");

        _connection.Closed += async (_) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
            await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", connectRequest));
        };

        await _connection.StartAsync();

        await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", connectRequest));
    }

    private async Task FetchViewersAsync()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("GetAllViewers"));
    }

    private async Task HandleScreenUpdate(byte[] screenData)
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

        _screenDataUrl = await module.InvokeAsync<string>("createImageBlobUrl", screenData);

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnLoad()
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

        var src = await module.InvokeAsync<string>("getElementAttribute", _screenImageElement, "src");

        await module.InvokeAsync<string>("revokeUrl", src);
    }

    private async Task ToggleInputEnabled(bool value)
    {
        _inputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendToggleInput", value));
        QueryParameterService.UpdateParameter("inputEnabled", value.ToString());
    }

    private async Task ToggleBlockUserInput(bool value)
    {
        _blockUserInput = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendBlockUserInput", value));
    }

    private async Task ToggleCursorTracking(bool value)
    {
        _cursorTracking = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendToggleCursorTracking", value));
        QueryParameterService.UpdateParameter("cursorTracking", value.ToString());
    }

    private async Task ChangeQuality(int quality)
    {
        _imageQuality = quality;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendImageQuality", quality));
        QueryParameterService.UpdateParameter("imageQuality", quality.ToString());
    }

    private async void OnChangeScreen(string display)
    {
        _selectedDisplay = display;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendSelectedScreen", display));
    }

    [JSInvokable]
    public void OnBeforeUnload()
    {
        Log.Information("OnBeforeUnload invoked for host {Host}", Host);

        Dispose();
    }

    public void Dispose()
    {
        _connection.DisposeAsync();
    }
}
