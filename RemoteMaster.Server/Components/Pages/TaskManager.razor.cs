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

public partial class TaskManager : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    private HubConnection _connection = null!;
    private string _searchQuery = string.Empty;
    private List<ProcessInfo> _processes = [];
    private string _processPath = string.Empty;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
        [
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        ]);

    private bool _isDarkMode = false;

    private readonly MudTheme _theme = new();

    protected async override Task OnInitializedAsync()
    {
        await InitializeHostConnectionAsync();
        await FetchProcesses();
    }

    private async Task FetchProcesses()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("GetRunningProcesses"));
    }

    private async Task InitializeHostConnectionAsync()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        var userId = UserManager.GetUserId(httpContext.User);

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/taskmanager", options =>
            {
                options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync(userId);
            })
            .AddMessagePackProtocol()
            .Build();

        _connection.On<IEnumerable<ProcessInfo>>("ReceiveRunningProcesses", async (processes) =>
        {
            _processes = processes.ToList();
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

    private async Task KillProcess(int processId)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("KillProcess", processId));
        await FetchProcesses();
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

    private bool FilterFunc1(ProcessInfo processInfo) => FilterFunc(processInfo, _searchQuery);

    private static bool FilterFunc(ProcessInfo processInfo, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return true;
        }

        if (int.TryParse(searchQuery, out var pid) && processInfo.Id == pid)
        {
            return true;
        }

        return processInfo.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private async Task StartProcess()
    {
        if (!string.IsNullOrWhiteSpace(_processPath))
        {
            await SafeInvokeAsync(() => _connection.InvokeAsync("StartProcess", _processPath));
            _processPath = string.Empty;
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await JsRuntime.InvokeVoidAsync("alert", "Please enter a valid process path.");
        }
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
