// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using Polly.Retry;
using RemoteMaster.Shared.Models;
using static MudBlazor.CategoryTypes;

namespace RemoteMaster.Server.Components.Pages;

public partial class TaskManager : IDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private HubConnection _connection;
    private string _searchQuery = string.Empty;
    private List<ProcessInfo> _processes = [];

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
        await FetchProcesses();
    }

    private async Task FetchProcesses()
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("GetRunningProcesses"));
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

        _connection.On<IEnumerable<ProcessInfo>>("ReceiveRunningProcesses", async (processes) =>
        {
            _processes = processes.ToList();
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

    private async Task KillProcess(int processId)
    {
        await SafeInvokeAsync(() => _connection.InvokeAsync("KillProcess", processId));
        await FetchProcesses();
        await InvokeAsync(StateHasChanged);
    }

    private string FormatSize(long size)
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

    [SuppressMessage("Performance", "CA1822:Пометьте члены как статические", Justification = "<Ожидание>")]
    private bool FilterFunc(ProcessInfo processInfo, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return true;
        }

        if (int.TryParse(searchQuery, out var pid) && processInfo.Id == pid)
        {
            return true;
        }

        if (processInfo.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
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
