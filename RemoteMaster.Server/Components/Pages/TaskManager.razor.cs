// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class TaskManager : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private string _searchQuery = string.Empty;
    private List<ProcessInfo> _processes = [];
    private List<ProcessInfo> _allProcesses = [];
    private string _processPath = string.Empty;
    private bool _firstRenderCompleted;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _firstRenderCompleted = true;
        }
    }

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        _user = authState.User;

        if (_user?.Identity?.IsAuthenticated == true)
        {
            await InitializeHostConnectionAsync();
            await FetchProcesses();
        }
    }

    private async Task FetchProcesses()
    {
        await SafeInvokeAsync(async () => await _connection!.InvokeAsync("GetRunningProcesses"));
    }

    private void FilterProcesses()
    {
        if (string.IsNullOrWhiteSpace(_searchQuery))
        {
            _processes = [.._allProcesses];
        }
        else
        {
            _processes = _allProcesses
                .Where(p => p.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private async Task UpdateSearchQuery(ChangeEventArgs e)
    {
        _searchQuery = e.Value?.ToString() ?? string.Empty;
        FilterProcesses();
        await InvokeAsync(StateHasChanged);
    }

    private async Task InitializeHostConnectionAsync()
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/taskmanager", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol()
            .Build();

        _connection.On<List<ProcessInfo>>("ReceiveRunningProcesses", async processes =>
        {
            _allProcesses = processes;
            _processes = [.. _allProcesses];

            FilterProcesses();

            await InvokeAsync(StateHasChanged);
        });

        _connection.Closed += async (_) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        await _connection.StartAsync();
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        var result = await ResiliencePipeline.ExecuteAsync(async cancellationToken =>
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

    private async Task KillProcess(int processId)
    {
        await SafeInvokeAsync(() => _connection?.InvokeAsync("KillProcess", processId) ?? Task.CompletedTask);
        await FetchProcesses();
        FilterProcesses();
        await InvokeAsync(StateHasChanged);
    }

    private static string FormatSize(long size)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = size;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private async Task StartProcess()
    {
        if (!string.IsNullOrWhiteSpace(_processPath))
        {
            await SafeInvokeAsync(() => _connection?.InvokeAsync("StartProcess", _processPath) ?? Task.CompletedTask);
            _processPath = string.Empty;
            await FetchProcesses();
            FilterProcesses();
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await JsRuntime.InvokeVoidAsync("alert", "Please enter a valid process path.");
        }
    }

    [JSInvokable]
    public async Task OnBeforeUnload()
    {
        await DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while asynchronously disposing the connection for host {Host}: {ex.Message}");
            }
        }

        GC.SuppressFinalize(this);
    }
}
