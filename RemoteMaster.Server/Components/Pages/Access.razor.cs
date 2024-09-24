// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.DTOs;
using RemoteMaster.Server.Requirements;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Formatters;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class Access : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private ClaimsPrincipal? _user;
    private string _transportType = string.Empty;
    private string? _screenDataUrl;
    private bool _drawerOpen;
    private HubConnection? _connection;
    private bool _isInputEnabled;
    private bool _isUserInputBlocked;
    private bool _drawCursor;
    private int _frameRate;
    private int _imageQuality;
    private string _operatingSystem = string.Empty;
    private string _dotNetVersion = string.Empty;
    private string _hostVersion = string.Empty;
    private List<Display> _displays = [];
    private List<string> _codecs = [];
    private string _selectedDisplay = string.Empty;
    private string? _selectedCodec = string.Empty;
    private ElementReference _screenImageElement;
    private List<ViewerDto> _viewers = [];
    private bool _isAccessDenied;

    private string? _title;
    private bool _isConnecting;
    private int _retryCount;
    private bool _firstRenderCompleted;
    private bool _disposed;

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
                SnackBar.Add("Access denied. You do not have permission to access this host.", Severity.Error);
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

        var frameRateResult = QueryParameterService.GetParameter("frameRate", 60);
        var imageQualityResult = QueryParameterService.GetParameter("imageQuality", 25);
        var drawCursorResult = QueryParameterService.GetParameter("drawCursor", false);
        var inputEnabledResult = QueryParameterService.GetParameter("inputEnabled", true);

        if (frameRateResult.IsSuccess)
        {
            _frameRate = frameRateResult.Value;
        }
        else
        {
            Log.Error("Error getting frame rate parameter: {Error}", frameRateResult.Errors.First().Message);
        }

        if (imageQualityResult.IsSuccess)
        {
            _imageQuality = imageQualityResult.Value;
        }
        else
        {
            Log.Error("Error getting image quality parameter: {Error}", imageQualityResult.Errors.First().Message);
        }

        if (drawCursorResult.IsSuccess)
        {
            _drawCursor = drawCursorResult.Value;
        }
        else
        {
            Log.Error("Error getting draw cursor parameter: {Error}", drawCursorResult.Errors.First().Message);
        }

        if (inputEnabledResult.IsSuccess)
        {
            _isInputEnabled = inputEnabledResult.Value;
        }
        else
        {
            Log.Error("Error getting input enabled parameter: {Error}", inputEnabledResult.Errors.First().Message);
        }

        if (newUri != uri.ToString())
        {
            NavigationManager.NavigateTo(newUri, true);
        }

        if (!_disposed && _connection is { State: HubConnectionState.Connected })
        {
            await UpdateServerParameters();
        }
    }

    private async Task UpdateServerParameters()
    {
        if (!_disposed && _connection is { State: HubConnectionState.Connected })
        {
            await _connection.InvokeAsync("SetFrameRate", _frameRate);
            await _connection.InvokeAsync("SetImageQuality", _imageQuality);
            await _connection.InvokeAsync("ToggleDrawCursor", _drawCursor);

            if (await IsPolicyPermittedAsync("ToggleInputPolicy"))
            {
                await _connection.InvokeAsync("ToggleInput", _isInputEnabled);
            }
        }
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task SafeInvokeAsync(Func<Task> action, bool requireAdmin = false)
    {
        var result = await ResiliencePipeline.ExecuteAsync(async _ =>
        {
            if (_connection is not { State: HubConnectionState.Connected })
            {
                throw new InvalidOperationException("Connection is not active");
            }

            if (requireAdmin && (_user == null || !_user.IsInRole("Administrator")))
            {
                await Task.CompletedTask;

                return "Failed";
            }

            await action();
            await Task.CompletedTask;

            return "Success";

        }, CancellationToken.None);

        if (result == "This function is not available in the current host version. Please update your host.")
        {
            if (_firstRenderCompleted)
            {
                await JsRuntime.InvokeVoidAsync("alert", result);
            }
        }
    }

    private List<DisplayDto> GetDisplayItems()
    {
        var displayItems = new List<DisplayDto>();

        for (var i = 0; i < _displays.Count; i++)
        {
            var resolution = $"{_displays[i].Resolution.Width} x {_displays[i].Resolution.Height}";
            var displayName = _displays[i].Name == "VIRTUAL_SCREEN"
                ? $"Virtual Screen ({resolution})"
                : $"Screen {i + 1} ({resolution})";

            displayItems.Add(new DisplayDto(_displays[i].Name, displayName));
        }

        return displayItems;
    }

    private async Task TerminateHost()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("TerminateHost"), true);
    }

    private async Task LockWorkStation()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("LockWorkStation"), true);
    }

    private async Task LogOffUser()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("LogOffUser", true), true);
    }

    private async Task SendCtrlAltDel()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendCommandToService", "CtrlAltDel"), true);
    }

    private async Task RebootHost()
    {
        if (_connection == null)
        {
            return;
        }

        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("RebootHost", powerActionRequest), true);
    }

    private async Task ShutdownHost()
    {
        if (_connection == null)
        {
            return;
        }

        var powerActionRequest = new PowerActionRequest
        {
            Message = string.Empty,
            Timeout = 0,
            ForceAppsClosed = true
        };

        await SafeInvokeAsync(() => _connection.InvokeAsync("ShutdownHost", powerActionRequest), true);
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

            var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

            _connection = new HubConnectionBuilder()
                .WithUrl($"https://{Host}:5001/hubs/control?screencast=true", options =>
                {
                    options.AccessTokenProvider = async () =>
                    {
                        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                        return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                    };
                })
                .AddMessagePackProtocol(options =>
                {
                    var resolver = CompositeResolver.Create([new IPAddressFormatter()], [ContractlessStandardResolver.Instance]);

                    options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
                })
                .Build();

            _connection.On<IEnumerable<Display>>("ReceiveDisplays", displays =>
            {
                _displays = displays.ToList();

                var primaryDisplay = _displays.FirstOrDefault(d => d.IsPrimary);

                if (primaryDisplay != null)
                {
                    _selectedDisplay = primaryDisplay.Name;
                }
            });

            _connection.On<IEnumerable<string>>("ReceiveAvailableCodecs", codecs =>
            {
                _codecs = codecs.ToList();

                _selectedCodec = _codecs.FirstOrDefault();
            });

            _connection.On<byte[]>("ReceiveScreenUpdate", HandleScreenUpdate);
            _connection.On<string>("ReceiveOperatingSystemVersion", operatingSystem => _operatingSystem = operatingSystem);
            _connection.On<Version>("ReceiveDotNetVersion", dotNetVersion => _dotNetVersion = dotNetVersion.ToString());
            _connection.On<Version>("ReceiveHostVersion", hostVersion => _hostVersion = hostVersion.ToString());
            _connection.On<string>("ReceiveTransportType", transportType => _transportType = transportType);

            _connection.On<List<ViewerDto>>("ReceiveAllViewers", viewers =>
            {
                _viewers = viewers;
                InvokeAsync(StateHasChanged);
            });

            _connection.Closed += async _ =>
            {
                if (!_disposed && _retryCount < 3)
                {
                    _retryCount++;

                    await Task.Delay(TimeSpan.FromSeconds(5));
                    await TryStartConnectionAsync();
                }
                else
                {
                    SnackBar.Add("Unable to reconnect. Please check the host status and try again later.", Severity.Error);
                }
            };

            await TryStartConnectionAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during host connection initialization for host {Host}", Host);
            SnackBar.Add("An error occurred while initializing the connection. Please try again later.", Severity.Error);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async Task TryStartConnectionAsync()
    {
        if (_connection == null)
        {
            return;
        }

        try
        {
            await _connection.StartAsync();
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP request error during connection to host {Host}", Host);
            SnackBar.Add("Unable to connect to the host. Please check the host status and try again later.", Severity.Error);
        }
        catch (TimeoutException ex)
        {
            Log.Error(ex, "Timeout error during connection to host {Host}", Host);
            SnackBar.Add("Connection to the host timed out. Please try again later.", Severity.Error);
        }
        catch (TaskCanceledException ex)
        {
            Log.Error(ex, "Task was canceled during connection to host {Host}", Host);
            SnackBar.Add("Connection to the host was canceled. Please try again later.", Severity.Error);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during connection to host {Host}", Host);
            SnackBar.Add("An error occurred while connecting to the host. Please try again later.", Severity.Error);
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

    private async Task EnableInput(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _isInputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("ToggleInput", value), true);
        QueryParameterService.UpdateParameter("inputEnabled", value.ToString());
    }

    private async Task BlockUserInput(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _isUserInputBlocked = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("BlockUserInput", value), true);
    }

    private async Task ToggleDrawCursor(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _drawCursor = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("ToggleDrawCursor", value));
        QueryParameterService.UpdateParameter("drawCursor", value.ToString());
    }

    private async Task ChangeFrameRate(int frameRate)
    {
        if (_connection == null)
        {
            return;
        }

        _frameRate = frameRate;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SetFrameRate", frameRate));
        QueryParameterService.UpdateParameter("frameRate", frameRate.ToString());
    }

    private async Task ChangeQuality(int quality)
    {
        if (_connection == null)
        {
            return;
        }

        _imageQuality = quality;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SetImageQuality", quality));
        QueryParameterService.UpdateParameter("imageQuality", quality.ToString());
    }

    private async void OnChangeScreen(string display)
    {
        if (_connection == null)
        {
            return;
        }

        _selectedDisplay = display;

        await SafeInvokeAsync(() => _connection.InvokeAsync("ChangeSelectedScreen", display));
    }

    private async void OnChangeCodec(string? codec)
    {
        if (_connection == null)
        {
            return;
        }

        _selectedCodec = codec;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SetCodec", codec));
    }

    private async Task DisconnectViewer(string connectionId)
    {
        if (_connection == null)
        {
            return;
        }

        await DialogService.ShowAsync<DisconnectViewerDialog>("Disconnect viewer", new DialogParameters
        {
            { nameof(DisconnectViewerDialog.HubConnection), _connection },
            { nameof(DisconnectViewerDialog.Viewer), _viewers.FirstOrDefault(v => v.ConnectionId == connectionId) }
        });
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
        if (_disposed)
        {
            return;
        }

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

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
