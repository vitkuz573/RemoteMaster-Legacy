// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Retry;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    private string? _screenDataUrl;
    private bool _drawerOpen;
    private HubConnection _connection = null!;
    private bool _inputEnabled;
    private bool _cursorTracking;
    private int _imageQuality;
    private string _hostVersion = string.Empty;
    private List<Display> _displays = [];
    private string _selectedDisplay = string.Empty;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
        [
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10)
        ]);

    private bool _isDarkMode = true;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthRight = "300px"
        }
    };

    private string? _title;

    protected override void OnParametersSet()
    {
        if (string.IsNullOrEmpty(_title))
        {
            _title = Host;
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("preventDefaultForKeydownWhenDrawerClosed", _drawerOpen);
    }

    protected async override Task OnInitializedAsync()
    {
        await InitializeHostConnectionAsync();
        await SetParametersFromUriAsync();
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
        await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await action();
                }
                catch (HubException ex) when (ex.Message.Contains("Method does not exist"))
                {
                    await JsRuntime.InvokeVoidAsync("alert", "This function is not available in the current host version. Please update your host.");
                }
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
        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/control", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var currentAccessToken = HttpContextAccessor.HttpContext.Request.Cookies["accessToken"];
                    var refreshToken = HttpContextAccessor.HttpContext.Request.Cookies["refreshToken"];

                    if (currentAccessToken == null)
                    {
                        if (await AuthService.RefreshTokensAsync(refreshToken))
                        {
                            currentAccessToken = HttpContextAccessor.HttpContext.Request.Cookies["accessToken"];
                        }
                    }

                    return currentAccessToken;
                };
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

        _connection.Closed += async (_) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
            await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", Intention.ManageDevice));
        };

        await _connection.StartAsync();

        await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", Intention.ManageDevice));
    }

    private async Task HandleScreenUpdate(byte[] screenData)
    {
        _screenDataUrl = await JsRuntime.InvokeAsync<string>("createImageBlobUrl", screenData);

        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleInputEnabled(bool value)
    {
        _inputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendToggleInput", value));
        QueryParameterService.UpdateParameter("inputEnabled", value.ToString());
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
        Dispose();
    }

    public void Dispose()
    {
        _connection.DisposeAsync();
    }
}
