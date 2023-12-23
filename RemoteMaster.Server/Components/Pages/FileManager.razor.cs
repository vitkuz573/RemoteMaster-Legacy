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

namespace RemoteMaster.Server.Components.Pages;

public partial class FileManager : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private HubConnection _connection;
    private string _currentPath = @"C:\";
    private List<string> _files = [];

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
    };

    protected async override Task OnInitializedAsync()
    {
        await InitializeHostConnectionAsync();

        await SafeInvokeAsync(() => _connection.InvokeAsync("GetFiles", _currentPath));
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

        _connection.On<List<string>>("ReceiveFiles", async (files) =>
        {
            _files = files;
            await InvokeAsync(StateHasChanged);
        });

        _connection.Closed += async (error) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        await _connection.StartAsync();
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

    private async Task DownloadFile(string fileName)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("DownloadFile", $"{_currentPath}/{fileName}"));
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
