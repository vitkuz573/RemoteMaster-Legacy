﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using RemoteMaster.Server.Components.Dialogs;
using RemoteMaster.Server.DTOs;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Models;

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
    private bool _isUserInputEnabled = true;
    private bool _isCursorVisible;
    private int _frameRate;
    private int _imageQuality;
    private string _operatingSystem = string.Empty;
    private Version _dotNetVersion = new();
    private Version _hostVersion = new();
    private List<Display> _displays = [];
    private List<string> _codecs = [];
    private string _selectedDisplay = string.Empty;
    private string? _selectedCodec = string.Empty;
    private bool _isAudioStreaming;
    private ElementReference _screenImageElement;
    private List<ViewerDto> _viewers = [];
    private bool _isAccessDenied;

    private string? _title;
    private bool _isConnecting;
    private bool _connectionFailed;
    private int _retryCount;
    private bool _firstRenderCompleted;
    private bool _disposed;

    private IJSObjectReference? _blobUtilsModule;
    private IJSObjectReference? _audioUtilsModule;
    private IJSObjectReference? _clipboardModule;
    private IJSObjectReference? _eventListenersModule;

    private string? _accessToken;

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

        var result = await HostAccessService.InitializeAccessAsync(Host, _user);

        _isAccessDenied = result.IsAccessDenied;

        if (_isAccessDenied && result.ErrorMessage != null)
        {
            SnackBar.Add(result.ErrorMessage, Severity.Error);
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed && !_isAccessDenied)
        {
            _firstRenderCompleted = true;

            try
            {
                var objectReference = DotNetObjectReference.Create(this);

                _eventListenersModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/eventListeners.js");

                await _eventListenersModule.InvokeVoidAsync("addPreventCtrlSListener");
                await _eventListenersModule.InvokeVoidAsync("addBeforeUnloadListener", objectReference);
                await _eventListenersModule.InvokeVoidAsync("addKeyDownEventListener", objectReference);
                await _eventListenersModule.InvokeVoidAsync("addKeyUpEventListener", objectReference);
                await _eventListenersModule.InvokeVoidAsync("preventDefaultForKeydownWhenDrawerClosed", _drawerOpen);

                _blobUtilsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

                if (_isAccessDenied)
                {
                    SnackBar.Add("Access denied. You do not have permission to access this host.", Severity.Error);
                }
                else
                {
                    var uri = new Uri(NavigationManager.Uri);
                    var queryParams = QueryHelpers.ParseQuery(uri.Query);

                    if (!queryParams.TryGetValue("action", out var action) || string.IsNullOrEmpty(action))
                    {
                        SnackBar.Add("Invalid URL: 'action' parameter is missing.", Severity.Error);

                        return;
                    }

                    await InitializeHostConnectionAsync(action);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while importing JS modules.");
                SnackBar.Add("An error occurred while initializing the application. Please try again later.", Severity.Error);
            }
        }
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        var result = await ResiliencePipeline.ExecuteAsync(async _ =>
        {
            if (_connection is not { State: HubConnectionState.Connected })
            {
                throw new InvalidOperationException("Connection is not active");
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

    private async Task TerminateHostAsync()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("TerminateHost"));
    }

    private async Task LockWorkStationAsync()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("LockWorkStation"));
    }

    private async Task LogOffUserAsync()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("LogOffUser", true));
    }

    private async Task SendCtrlAltDelAsync()
    {
        if (_connection == null)
        {
            return;
        }

        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        var connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/service", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        await connection.StartAsync();

        await SafeInvokeAsync(() => connection.InvokeAsync("SendCommandToService", "CtrlAltDel"));

        await connection.StopAsync();
    }

    private async Task RebootHostAsync()
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

        await SafeInvokeAsync(() => _connection.InvokeAsync("RebootHost", powerActionRequest));
    }

    private async Task ShutdownHostAsync()
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

        await SafeInvokeAsync(() => _connection.InvokeAsync("ShutdownHost", powerActionRequest));
    }

    private async Task<bool> ShowSslWarningDialogAsync(IPAddress ipAddress, SslPolicyErrors sslPolicyErrors, CertificateInfo certificateInfo)
    {
        var parameters = new DialogParameters<SslWarningDialog>
        {
            { d => d.IpAddress, ipAddress },
            { d => d.SslPolicyErrors, sslPolicyErrors },
            { d => d.CertificateInfo, certificateInfo }
        };

        var dialog = await DialogService.ShowAsync<SslWarningDialog>("SSL Certificate Warning", parameters);
        var result = await dialog.Result;

        return !result?.Canceled ?? throw new InvalidOperationException("Result not found.");
    }

    private async Task InitializeHostConnectionAsync(string action)
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
                .WithUrl($"https://{Host}:5001/hubs/control?screencast=true&action={action}", options =>
                {
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            clientHandler.ServerCertificateCustomValidationCallback = (_, cert, chain, sslPolicyErrors) =>
                            {
                                var ipAddress = IPAddress.Parse(Host);

                                if (SslWarningService.IsSslAllowed(ipAddress))
                                {
                                    return true;
                                }

                                int keySize;

                                if (cert == null)
                                {
                                    return false;
                                }

                                switch (cert.PublicKey.Oid.Value)
                                {
                                    case "1.2.840.113549.1.1.1":
                                    {
                                        using var rsa = cert.GetRSAPublicKey();
                                        keySize = rsa?.KeySize ?? 0;
                                        break;
                                    }
                                    case "1.2.840.10040.4.1":
                                    {
                                        using var dsa = cert.GetDSAPublicKey();
                                        keySize = dsa?.KeySize ?? 0;
                                        break;
                                    }
                                    case "1.2.840.10045.2.1":
                                    {
                                        using var ecdsa = cert.GetECDsaPublicKey();
                                        keySize = ecdsa?.KeySize ?? 0;
                                        break;
                                    }
                                    default:
                                    {
                                        keySize = 0;
                                        break;
                                    }
                                }

                                var certificateInfo = new CertificateInfo(
                                    cert.Issuer,
                                    cert.Subject,
                                    cert.GetExpirationDateString(),
                                    cert.GetEffectiveDateString(),
                                    cert.SignatureAlgorithm.FriendlyName ?? "Unknown",
                                    keySize.ToString(),
                                    chain?.ChainElements.Select(e => e.Certificate.Subject).ToList() ?? []
                                );

                                return sslPolicyErrors == SslPolicyErrors.None || Task.Run(() => ShowSslWarningDialogAsync(ipAddress, sslPolicyErrors, certificateInfo)).Result;
                            };
                        }

                        return handler;
                    };

                    options.AccessTokenProvider = async () =>
                    {
                        var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                        return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                    };
                })
                .AddMessagePackProtocol(options => options.Configure())
                .Build();

            _connection.On<IEnumerable<Display>>("ReceiveDisplays", async displays =>
            {
                _displays = [.. displays];

                var primaryDisplay = _displays.FirstOrDefault(d => d.IsPrimary);

                if (primaryDisplay == null)
                {
                    return;
                }

                await OnChangeScreenAsync(primaryDisplay.Name);
            });

            _connection.On<IEnumerable<string>>("ReceiveAvailableCodecs", async codecs =>
            {
                _codecs = [.. codecs];

                await OnChangeCodecAsync(_codecs.FirstOrDefault());
            });

            _connection.On<byte[]>("ReceiveScreenUpdate", HandleScreenUpdateAsync);
            _connection.On<string>("ReceiveAudioUpdate", HandleAudioUpdateBase64Async);
            _connection.On<string>("ReceiveClipboard", HandleReceiveClipboardAsync);
            _connection.On<string>("ReceiveOperatingSystemVersion", operatingSystem => _operatingSystem = operatingSystem);
            _connection.On<Version>("ReceiveDotNetVersion", dotNetVersion => _dotNetVersion = dotNetVersion);
            _connection.On<Version>("ReceiveHostVersion", hostVersion => _hostVersion = hostVersion);
            _connection.On<string>("ReceiveTransportType", transportType => _transportType = transportType);

            _connection.On<List<ViewerDto>>("ReceiveAllViewers", viewers =>
            {
                _viewers = viewers;
                InvokeAsync(StateHasChanged);
            });

            _connection.On<Message>("ReceiveMessage", async message =>
            {
                var snackBarSeverity = message.Severity switch
                {
                    Message.MessageSeverity.Information => Severity.Info,
                    Message.MessageSeverity.Warning => Severity.Warning,
                    Message.MessageSeverity.Error => Severity.Error
                };

                SnackBar.Add(message.Text, snackBarSeverity);

                if (!string.IsNullOrEmpty(message.Meta))
                {
                    switch (message.Meta)
                    {
                        case MessageMeta.ConnectionError:
                        case MessageMeta.AuthorizationError:
                        case MessageMeta.ScreencastError:
                            _connectionFailed = true;
                            await InvokeAsync(StateHasChanged);
                            break;
                    }
                }
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
                    _connectionFailed = true;
                    await InvokeAsync(StateHasChanged);
                    SnackBar.Add("Unable to reconnect. Please check the host status and try again later.", Severity.Error);
                }
            };

            await TryStartConnectionAsync();

            await SetImageQualityAsync(25);
            await SetFrameRateAsync(60);

            switch (action)
            {
                case "control":
                    await ToggleIsCursorVisibleAsync(false);
                    await ToggleInputAsync(true);
                    break;
                case "view":
                    await ToggleIsCursorVisibleAsync(true);
                    await ToggleInputAsync(false);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred during host connection initialization for host {Host}", Host);
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
            Logger.LogError(ex, "HTTP request error during connection to host {Host}", Host);
            SnackBar.Add("Unable to connect to the host. Please check the host status and try again later.", Severity.Error);
            _connectionFailed = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (TimeoutException ex)
        {
            Logger.LogError(ex, "Timeout error during connection to host {Host}", Host);
            SnackBar.Add("Connection to the host timed out. Please try again later.", Severity.Error);
            _connectionFailed = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogError(ex, "Task was canceled during connection to host {Host}", Host);
            SnackBar.Add("Connection to the host was canceled. Please try again later.", Severity.Error);
            _connectionFailed = true;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred during connection to host {Host}", Host);
            SnackBar.Add("An error occurred while connecting to the host. Please try again later.", Severity.Error);
            _connectionFailed = true;
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private async Task HandleScreenUpdateAsync(byte[] screenData)
    {
        if (!_disposed && _firstRenderCompleted)
        {
            _blobUtilsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

            _screenDataUrl = await _blobUtilsModule.InvokeAsync<string>("createImageBlobUrl", screenData);

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleAudioUpdateBase64Async(string base64Data)
    {
        if (!_disposed && _firstRenderCompleted)
        {
            _audioUtilsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/audioUtils.js");

            await _audioUtilsModule.InvokeVoidAsync("playAudioChunk", base64Data);
        }
    }

    private async Task HandleReceiveClipboardAsync(string text)
    {
        if (!_disposed && _firstRenderCompleted)
        {
            _clipboardModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/clipboardUtils.js");

            await _clipboardModule.InvokeVoidAsync("copyTextToClipboard", text);
        }
    }

    private async Task OnLoadAsync()
    {
        if (!_disposed && _firstRenderCompleted)
        {
            _blobUtilsModule ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/blobUtils.js");

            var src = await _blobUtilsModule.InvokeAsync<string>("getElementAttribute", _screenImageElement, "src");

            await _blobUtilsModule.InvokeVoidAsync("revokeUrl", src);
        }
    }

    private async Task ToggleInputAsync(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _isInputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("ToggleInput", value));
    }

    private async Task ToggleUserInputAsync(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _isUserInputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("BlockUserInput", !value));
    }

    private async Task ToggleAudioStreamingAsync(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _isAudioStreaming = value;

        if (value)
        {
            _audioUtilsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/audioUtils.js");

            await _audioUtilsModule.InvokeVoidAsync("initAudioContext");

            await SafeInvokeAsync(() => _connection.InvokeAsync("StartAudioStreaming"));
        }
        else
        {
            await SafeInvokeAsync(() => _connection.InvokeAsync("StopAudioStreaming"));
        }
    }

    private async Task ToggleIsCursorVisibleAsync(bool value)
    {
        if (_connection == null)
        {
            return;
        }

        _isCursorVisible = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("ToggleIsCursorVisible", value));
    }

    private async Task SetFrameRateAsync(int frameRate)
    {
        if (_connection == null)
        {
            return;
        }

        _frameRate = frameRate;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SetFrameRate", frameRate));
    }

    private async Task SetImageQualityAsync(int quality)
    {
        if (_connection == null)
        {
            return;
        }

        _imageQuality = quality;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SetImageQuality", quality));
    }

    private async Task OnChangeScreenAsync(string display)
    {
        if (_connection == null)
        {
            return;
        }

        _selectedDisplay = display;

        await SafeInvokeAsync(() => _connection.InvokeAsync("ChangeSelectedScreen", display));
    }

    private async Task OnChangeCodecAsync(string? codec)
    {
        if (_connection == null)
        {
            return;
        }

        _selectedCodec = codec;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SetCodec", codec));
    }

    private async Task DisconnectViewerAsync(string connectionId)
    {
        var viewer = _viewers.FirstOrDefault(v => v.ConnectionId == connectionId) ?? throw new InvalidOperationException($"Viewer with Connection ID {connectionId} not found.");
        
        if (_user == null)
        {
            throw new InvalidOperationException("User not initialized.");
        }

        if (viewer.UserName == _user.FindFirstValue(ClaimTypes.Name))
        {
            SnackBar.Add("You cannot disconnect your own session.", Severity.Error);

            return;
        }

        if (_connection == null)
        {
            return;
        }

        await DialogService.ShowAsync<DisconnectViewerDialog>("Disconnect viewer", new DialogParameters
        {
            { nameof(DisconnectViewerDialog.HubConnection), _connection },
            { nameof(DisconnectViewerDialog.Viewer), viewer }
        });
    }
    

    [JSInvokable]
    public async Task OnBeforeUnloadAsync()
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
                Logger.LogError(ex, "An error occurred while asynchronously disposing the connection for host {Host}", Host);
            }
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
