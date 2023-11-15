// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.JSInterop;
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Pages;

public partial class Connect : IDisposable
{
    [Parameter]
    public string Host { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private IJSRuntime JSRuntime { get; set; }

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; }

    private string? _screenDataUrl;

    private bool _isMenuOpen = false;

    private string _hostVersion;

    private bool _inputEnabled;
    private bool _cursorTracking;
    private int _imageQuality;

    private List<Display> _displays;

    private string _newUri;

    private string _selectedDisplay;

    private HubConnection _connection;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        });

    protected async override Task OnInitializedAsync()
    {
        await InitializeHostConnectionAsync();
        await SetParametersFromUriAsync();
    }

    private async Task SetParametersFromUriAsync()
    {
        var uri = new Uri(NavigationManager.Uri);
        var queryParameters = QueryHelpers.ParseQuery(uri.Query);
        var newUri = uri.ToString();

        _imageQuality = GetQueryParameter(queryParameters, "imageQuality", 25, ref newUri);
        _cursorTracking = GetQueryParameter(queryParameters, "cursorTracking", false, ref newUri);
        _inputEnabled = GetQueryParameter(queryParameters, "inputEnabled", true, ref newUri);

        if (newUri != uri.ToString())
        {
            NavigationManager.NavigateTo(newUri, true);
        }

        await UpdateServerParameters();
    }

    private static T GetQueryParameter<T>(IDictionary<string, StringValues> queryParameters, string key, T defaultValue, ref string updatedUri)
    {
        if (!queryParameters.TryGetValue(key, out var valueString) || !TryParse(valueString, out T value))
        {
            value = defaultValue;
            updatedUri = QueryHelpers.AddQueryString(updatedUri, key, value.ToString());
        }

        return value;
    }

    private static bool TryParse<T>(StringValues stringValue, out T result)
    {
        if (typeof(T) == typeof(int))
        {
            if (int.TryParse(stringValue, out var intValue))
            {
                result = (T)(object)intValue;

                return true;
            }
        }
        else if (typeof(T) == typeof(bool))
        {
            if (bool.TryParse(stringValue, out var boolValue))
            {
                result = (T)(object)boolValue;

                return true;
            }
        }

        result = default;

        return false;
    }

    private async Task UpdateServerParameters()
    {
        await _connection.InvokeAsync("SendImageQuality", _imageQuality);
        await _connection.InvokeAsync("SendToggleCursorTracking", _cursorTracking);
        await _connection.InvokeAsync("SendToggleInput", _inputEnabled);
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
                    await JSRuntime.InvokeVoidAsync("showAlert", "This function is not available in the current host version. Please update your host.");
                }
            }
            else
            {
                throw new InvalidOperationException("Connection is not active");
            }
        });
    }

    private static async Task<string> TryGetDnsNameOrFallbackToIpAsync(string host)
    {
        try
        {
            var ipHostEntry = await Dns.GetHostEntryAsync(host);

            return ipHostEntry.HostName;
        }
        catch
        {
            return host;
        }
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var displayHost = await TryGetDnsNameOrFallbackToIpAsync(Host);
            await JSRuntime.InvokeVoidAsync("setTitle", displayHost);

            if (!string.IsNullOrEmpty(_newUri))
            {
                await JSRuntime.InvokeVoidAsync("eval", $"history.replaceState(null, '', '{_newUri}');");
            }

            await SetupEventListeners();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task HandleScreenUpdate(byte[] screenData)
    {
        _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", screenData);

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetupEventListeners()
    {
        await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addBeforeUnloadListener", DotNetObjectReference.Create(this));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var accessToken = httpContext.Request.Cookies["accessToken"];

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5076/hubs/control", options =>
            {
                options.Headers.Add("Authorization", $"Bearer {accessToken}");
            })
            .AddMessagePackProtocol()
            .Build();

        _connection.On<IEnumerable<Display>>("ReceiveDisplays", (displays) => _displays = displays.ToList());
        _connection.On<byte[]>("ReceiveScreenUpdate", HandleScreenUpdate);
        _connection.On<Version>("ReceiveHostVersion", version => _hostVersion = version.ToString());

        _connection.Closed += async (error) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
            await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", Intention.Connect));
        };

        await _connection.StartAsync();

        await SafeInvokeAsync(() => _connection.InvokeAsync("ConnectAs", Intention.Connect));
    }

    private async Task<PointD> GetRelativeMousePositionPercentAsync(MouseEventArgs e)
    {
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DOMRect>("getBoundingClientRect");
        var percentX = (e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (e.ClientY - imgPosition.Top) / imgPosition.Height;

        return new PointD(percentX, percentY);
    }

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;
    }

    private async void OnChangeScreen(string display)
    {
        _selectedDisplay = display;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendSelectedScreen", display));
    }

    private async Task ChangeQuality(int quality)
    {
        _imageQuality = quality;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendImageQuality", quality));
        await UpdateUrlParameter("imageQuality", quality.ToString());
    }

    private async Task ToggleCursorTracking(bool value)
    {
        _cursorTracking = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendToggleCursorTracking", value));
        await UpdateUrlParameter("cursorTracking", value.ToString());
    }

    private async Task ToggleInputEnabled(bool value)
    {
        _inputEnabled = value;

        await SafeInvokeAsync(() => _connection.InvokeAsync("SendToggleInput", value));
        await UpdateUrlParameter("inputEnabled", value.ToString());
    }

    private async Task UpdateUrlParameter(string key, string value)
    {
        var uri = new Uri(NavigationManager.Uri);
        var queryParameters = QueryHelpers.ParseQuery(uri.Query);

        if (queryParameters.ContainsKey(key))
        {
            queryParameters[key] = value;
        }
        else
        {
            queryParameters.Add(key, value);
        }

        var newUri = QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path), queryParameters);
        await JSRuntime.InvokeVoidAsync("history.replaceState", null, "", newUri);
    }

    private async Task KillHost()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendKillHost"));
    }

    private async Task RebootComputer()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendRebootComputer", string.Empty, 0, true));
    }

    private async Task ShutdownComputer()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendShutdownComputer", string.Empty, 0, true));
    }

    private async Task SendCtrlAltDel()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendCommandToService", "CtrlAltDel"));
    }

    [JSInvokable]
    public void OnBeforeUnload()
    {
        Dispose();
    }

    public void Dispose()
    {
        _connection?.DisposeAsync();
    }
}