// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Wrap;
using RemoteMaster.Server.Data;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

public partial class Access : IAsyncDisposable
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
    private bool _accessDenied = false;

    private readonly AsyncPolicyWrap _combinedPolicy;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthRight = "370px"
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
            await module.InvokeVoidAsync("preventDefaultForKeydownWhenDrawerClosed", _drawerOpen);

            await InitializeHostConnectionAsync();
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

        if (_connection != null)
        {
            await UpdateServerParameters();
        }
    }

    private async Task UpdateServerParameters()
    {
        if (_connection != null)
        {
            await _connection.InvokeAsync("SendImageQuality", _imageQuality);
            await _connection.InvokeAsync("SendToggleCursorTracking", _cursorTracking);

            if (HasPermission("ToggleInput"))
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
                if (!await HasAccessAsync())
                {
                    Snackbar.Add("Access denied. You do not have permission to access this computer.", Severity.Error);
                    _accessDenied = true;

                    await InvokeAsync(StateHasChanged);

                    return;
                }

                if (requireAdmin && !IsUserInRole("Administrator"))
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

    private async Task KillHost()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKillHost"), true);
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
        if (!await HasAccessAsync())
        {
            Snackbar.Add("Access denied. You do not have permission to access this computer.", Severity.Error);
            _accessDenied = true;

            await InvokeAsync(StateHasChanged);

            return;
        }

        var httpContext = HttpContextAccessor.HttpContext;
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _accessToken = await AccessTokenProvider.GetAccessTokenAsync(userId);

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

        _connection.On<List<ViewerDto>>("ReceiveAllViewers", viewers =>
        {
            _viewers = viewers;

            InvokeAsync(StateHasChanged);
        });

        var userIdentity = httpContext?.User.Identity as ClaimsIdentity ?? throw new InvalidOperationException("User identity is not a ClaimsIdentity.");
        var role = userIdentity.Claims
                        .FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        var connectionRequest = new ConnectionRequest(Intention.ManageDevice, userIdentity.Name, role);

        _connection.Closed += async (_) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
            await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", connectionRequest));
        };

        await _connection.StartAsync();

        await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", connectionRequest));
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

    [Authorize(Policy = "ToggleInputPolicy")]
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

    private bool IsUserInRole(string role)
    {
        var userRoles = new List<string>();

        var httpContext = HttpContextAccessor.HttpContext;
        var rolesClaim = httpContext?.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(rolesClaim))
        {
            userRoles = [.. rolesClaim.Split(',')];
        }

        return userRoles.Contains(role);
    }

    private bool HasPermission(string permission)
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var permissionClaim = httpContext?.User?.Claims.FirstOrDefault(c => c.Type == "Permission" && c.Value == permission);

        return permissionClaim != null;
    }

    private async Task<bool> HasAccessAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var httpContext = HttpContextAccessor.HttpContext;
        var userPrincipal = httpContext?.User;

        if (userPrincipal == null)
        {
            return false;
        }

        var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var computer = await dbContext.Computers
            .Include(c => c.Parent)
            .FirstOrDefaultAsync(c => c.Name == Host || c.IpAddress == Host);

        if (computer?.Parent == null)
        {
            return false;
        }

        var organizationalUnitId = computer.ParentId.Value;

        return await dbContext.OrganizationalUnits
            .Where(ou => ou.NodeId == organizationalUnitId && ou.AccessibleUsers.Any(u => u.Id == userId))
            .AnyAsync();
    }

    [JSInvokable]
    public async Task OnBeforeUnload()
    {
        Log.Information("OnBeforeUnload invoked for host {Host}", Host);

        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while asynchronously disposing the connection for host {Host}", Host);
        }
    }
}
