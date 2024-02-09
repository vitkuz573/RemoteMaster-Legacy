// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Retry;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;
using PointD = (double, double);

namespace RemoteMaster.Server.Components.Pages;

public partial class Access : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    private string? _screenDataUrl;
    private bool _drawerOpen = false;
    private HubConnection _connection = null!;
    private bool _inputEnabled;
    private bool _cursorTracking;
    private int _imageQuality;
    private string _hostVersion = string.Empty;
    private List<Display> _displays = [];
    private string _selectedDisplay = string.Empty;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(7),
                TimeSpan.FromSeconds(10),
        });

    private bool _isDarkMode = true;

    private readonly MudTheme _theme = new()
    {
        LayoutProperties = new LayoutProperties()
        {
            DrawerWidthRight = "300px"
        }
    };

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupEventListeners();
        }
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
                    await JSRuntime.InvokeVoidAsync("showAlert", "This function is not available in the current host version. Please update your host.");
                }
            }
            else
            {
                throw new InvalidOperationException("Connection is not active");
            }
        });
    }

    private async Task<PointD> GetRelativeMousePositionPercentAsync(MouseEventArgs e)
    {
        var imgElement = await JSRuntime.InvokeAsync<IJSObjectReference>("document.getElementById", "screenImage");
        var imgPosition = await imgElement.InvokeAsync<DomRect>("getBoundingClientRect");
        var percentX = (e.ClientX - imgPosition.Left) / imgPosition.Width;
        var percentY = (e.ClientY - imgPosition.Top) / imgPosition.Height;

        return new PointD(percentX, percentY);
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
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendRebootComputer", string.Empty, 0, true));
    }

    private async Task ShutdownComputer()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("SendShutdownComputer", string.Empty, 0, true));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
        var accessToken = httpContext.Request.Cookies["accessToken"];
        
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("Access token is not available.");
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/control", options =>
            {
                options.Headers.Add("Authorization", $"Bearer {accessToken}");
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

            InvokeAsync(StateHasChanged);
        });

        _connection.On<byte[]>("ReceiveScreenUpdate", HandleScreenUpdate);
        _connection.On<Version>("ReceiveHostVersion", version => _hostVersion = version.ToString());

        _connection.Closed += async (error) =>
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
        _screenDataUrl = await JSRuntime.InvokeAsync<string>("createImageBlobUrl", screenData);

        await InvokeAsync(StateHasChanged);
    }

    private async Task SetupEventListeners()
    {
        await JSRuntime.InvokeVoidAsync("addKeyDownEventListener", DotNetObjectReference.Create(this));
        await JSRuntime.InvokeVoidAsync("addKeyUpEventListener", DotNetObjectReference.Create(this));
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
        _connection?.DisposeAsync();
    }
}
