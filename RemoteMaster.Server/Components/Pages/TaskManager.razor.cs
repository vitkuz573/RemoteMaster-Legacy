using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Polly;
using Polly.Retry;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class TaskManager : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private string _searchQuery = string.Empty;
    private List<ProcessInfo> _processes = [];
    private List<ProcessInfo> _allProcesses = [];
    private string _processPath = string.Empty;

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
        [
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(7),
            TimeSpan.FromSeconds(10),
        ]);

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
            _processes = new List<ProcessInfo>(_allProcesses);
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
        var userId = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"https://{Host}:5001/hubs/taskmanager", options =>
                {
                    options.AccessTokenProvider = async () => await AccessTokenProvider.GetAccessTokenAsync(userId);
                })
                .AddMessagePackProtocol()
                .Build();

            _connection.On<List<ProcessInfo>>("ReceiveRunningProcesses", async (processes) =>
            {
                _allProcesses = processes ?? [];
                _processes = new List<ProcessInfo>(_allProcesses);
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
    }

    private async Task SafeInvokeAsync(Func<Task> action)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            if (_connection?.State == HubConnectionState.Connected)
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
                Console.Error.WriteLine($"An error occurred while asynchronously disposing the connection for host {Host}: {ex.Message}");
            }
        }

        GC.SuppressFinalize(this);
    }
}
