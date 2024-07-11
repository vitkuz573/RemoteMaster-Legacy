// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Wrap;
using RemoteMaster.Server.Models;
using RemoteMaster.Server.Requirements;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private ClaimsPrincipal? _user;
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
    private string? _accessToken;
    private List<ViewerDto> _viewers = [];
    private bool _isAccessDenied = true;
    private readonly AsyncPolicyWrap _combinedPolicy;

    private string? _title;
    private bool _isConnecting;
    private int _retryCount;
    private bool _firstRenderCompleted = false;
    private bool _disposed = false;

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

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        _user = authState.User;
        _isAccessDenied = _user == null || !await HasAccessAsync();
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed && !_isAccessDenied)
        {
            _firstRenderCompleted = true;
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/eventListeners.js");

            await module.InvokeVoidAsync("addPreventCtrlSListener");
            await module.InvokeVoidAsync("addBeforeUnloadListener", DotNetObjectReference.Create(this));
            await module.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
            await module.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
            await module.InvokeVoidAsync("preventDefaultForKeydownWhenDrawerClosed", _drawerOpen);

            if (_isAccessDenied)
            {
                Snackbar.Add("Access denied. You do not have permission to access this computer.", Severity.Error);
            }
            else
            {
                await InitializeHostConnectionAsync();
            }

            await SetParametersFromUriAsync();
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

        if (!_disposed && _connection != null && _connection.State == HubConnectionState.Connected)
        {
            await UpdateServerParameters();
        }
    }

    private async Task UpdateServerParameters()
    {
        if (!_disposed && _connection != null && _connection.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendImageQuality", _imageQuality);
            await _connection.InvokeAsync("SendToggleCursorTracking", _cursorTracking);

            if (await IsPolicyPermittedAsync("ToggleInputPolicy"))
            {
                await _connection.InvokeAsync("SendToggleInput", _inputEnabled);
            }
        }
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task SafeInvokeAsync(Func<Task> action, bool requireAdmin = false)
    {
        await _combinedPolicy.ExecuteAsync(async () =>
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                if (requireAdmin && (_user == null || !_user.IsInRole("Administrator")))
                {
                    return;
                }

                await action();
            }
            else
            {
                throw new InvalidOperationException("Connection is not active");
            }
        });
    }

    private List<DisplayItem> GetDisplayItems()
    {
        var displayItems = new List<DisplayItem>();

        for (var i = 0; i < _displays.Count; i++)
        {
            var resolution = $"{_displays[i].Resolution.Width} x {_displays[i].Resolution.Height}";
            var displayName = _displays[i].Name == "VIRTUAL_SCREEN"
                ? $"Virtual Screen ({resolution})"
                : $"Screen {i + 1} ({resolution})";

            displayItems.Add(new DisplayItem
            {
                Name = _displays[i].Name,
                DisplayName = displayName
            });
        }

        return displayItems;
    }

    private async Task KillHost()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKillHost"), true);
    }

    private async Task LockWorkStation()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendLockWorkStation"), true);
    }

    private async Task LogOffUser()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendLogOffUser", true), true);
    }

    private async Task SendCtrlAltDel()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendCommandToService", "CtrlAltDel"), true);
    }

    private async Task RebootComputer()
    {
        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendRebootComputer", powerActionRequest), true);
    }

    private async Task ShutdownComputer()
    {
        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendShutdownComputer", powerActionRequest), true);
    }

    private async Task InitializeHostConnectionAsync()
    {
        try
        {
            if (_isConnecting)
            {
                return;
            }

            _isConnecting = true;
            _retryCount = 0;

            var userId = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID is missing.");
            }

            _accessToken = await AccessTokenProvider.GetAccessTokenAsync(userId);

            _connection = new HubConnectionBuilder()
                .WithUrl($"https://{Host}:5001/hubs/control?screencast=true", options =>
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

            _connection.On<List<ViewerDto>>("ReceiveAllViewers", viewers =>
            {
                _viewers = viewers;
                InvokeAsync(StateHasChanged);
            });

            var userIdentity = _user?.Identity as ClaimsIdentity ?? throw new InvalidOperationException("User identity is not a ClaimsIdentity.");

            var userName = userIdentity.Name;
            var role = userIdentity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(role))
            {
                throw new InvalidOperationException("User name or role is missing.");
            }

            _connection.Closed += async (_) =>
            {
                if (!_disposed && _retryCount < 3)
                {
                    _retryCount++;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await TryStartConnectionAsync();
                }
                else
                {
                    Snackbar.Add("Unable to reconnect. Please check the host status and try again later.", Severity.Error);
                }
            };

            await TryStartConnectionAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during host connection initialization for host {Host}", Host);
            Snackbar.Add("An error occurred while initializing the connection. Please try again later.", Severity.Error);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async Task TryStartConnectionAsync()
    {
        try
        {
            await _connection.StartAsync();
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP request error during connection to host {Host}", Host);
            Snackbar.Add("Unable to connect to the host. Please check the host status and try again later.", Severity.Error);
        }
        catch (TimeoutException ex)
        {
            Log.Error(ex, "Timeout error during connection to host {Host}", Host);
            Snackbar.Add("Connection to the host timed out. Please try again later.", Severity.Error);
        }
        catch (TaskCanceledException ex)
        {
            Log.Error(ex, "Task was canceled during connection to host {Host}", Host);
            Snackbar.Add("Connection to the host was canceled. Please try again later.", Severity.Error);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during connection to host {Host}", Host);
            Snackbar.Add("An error occurred while connecting to the host. Please try again later.", Severity.Error);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async Task HandleScreenUpdate(byte[] screenData)
    {
        if (!_disposed && _firstRenderCompleted)
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

            _screenDataUrl = await module.InvokeAsync<string>("createImageBlobUrl", screenData);

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnLoad()
    {
        if (!_disposed && _firstRenderCompleted)
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

            var src = await module.InvokeAsync<string>("getElementAttribute", _screenImageElement, "src");

            await module.InvokeAsync<string>("revokeUrl", src);
        }
    }

    private async Task ToggleInputEnabled(bool value)
    {
        _inputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendToggleInput", value), true);
        QueryParameterService.UpdateParameter("inputEnabled", value.ToString());
    }

    private async Task ToggleBlockUserInput(bool value)
    {
        _blockUserInput = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendBlockUserInput", value), true);
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

    private async Task<bool> IsPolicyPermittedAsync(string policyName)
    {
        if (_user == null)
        {
            return false;
        }

        var authorizationResult = await AuthorizationService.AuthorizeAsync(_user, Host, policyName);

        return authorizationResult.Succeeded;
    }

    private async Task<bool> HasAccessAsync()
    {
        if (_user == null)
        {
            return false;
        }

        var requirement = new HostAccessRequirement(Host);
        var requirements = new List<IAuthorizationRequirement> { requirement };

        var authorizationResult = await AuthorizationService.AuthorizeAsync(_user, Host, requirements);

        return authorizationResult.Succeeded;
    }

    [JSInvokable]
    public async Task OnBeforeUnload()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_connection != null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while asynchronously disposing the connection for host {Host}", Host);
            }
        }

        GC.SuppressFinalize(this);
    }
}
