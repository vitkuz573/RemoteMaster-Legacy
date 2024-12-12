// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using RemoteMaster.Shared.Extensions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class LogsViewer : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private List<string> _logFiles = [];
    private string? _selectedLogFile;
    private string _logContent = string.Empty;
    private string _selectedLogLevel = string.Empty;
    private DateTime? _startDate;
    private DateTime? _endDate;

    private bool _firstRenderCompleted;
    private bool _disposed;

    private bool _isAccessDenied;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_disposed && !_isAccessDenied)
        {
            _firstRenderCompleted = true;

            if (_isAccessDenied)
            {
                SnackBar.Add("Access denied. You do not have permission to access this host.", Severity.Error);
            }
            else
            {
                await InitializeHostConnectionAsync();
                await FetchLogFiles();
            }
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

    private async Task InitializeHostConnectionAsync()
    {
        var userId = _user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID is not found.");

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/log", options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var accessTokenResult = await AccessTokenProvider.GetAccessTokenAsync(userId);

                    return accessTokenResult.IsSuccess ? accessTokenResult.Value : null;
                };
            })
            .AddMessagePackProtocol(options => options.Configure())
            .Build();

        _connection.On<List<string>>("ReceiveLogFiles", logs =>
        {
            _logFiles = logs;

            InvokeAsync(StateHasChanged);
        });

        _connection.On<string>("ReceiveLog", logContent =>
        {
            _logContent = logContent;

            InvokeAsync(StateHasChanged);
        });

        _connection.On<Message>("ReceiveMessage", message =>
        {
            var snackBarSeverity = message.Severity switch
            {
                Message.MessageSeverity.Information => Severity.Info,
                Message.MessageSeverity.Warning => Severity.Warning,
                Message.MessageSeverity.Error => Severity.Error
            };

            SnackBar.Add(message.Text, snackBarSeverity);
        });

        await _connection.StartAsync();
    }

    private async Task FetchLogFiles()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("GetLogFiles"));
    }

    private async Task OnLogSelected(ChangeEventArgs e)
    {
        _selectedLogFile = e.Value?.ToString();
        _logContent = string.Empty;

        await FetchLogs();
    }

    private async Task FetchLogs()
    {
        if (_connection == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_selectedLogFile))
        {
            await SafeInvokeAsync(() => _connection.InvokeAsync("GetLog", _selectedLogFile));
        }
    }

    private async Task FetchFilteredLogs()
    {
        if (_connection == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(_selectedLogFile))
        {
            await SafeInvokeAsync(() => _connection.InvokeAsync("GetFilteredLog", _selectedLogFile, _selectedLogLevel, _startDate, _endDate));
        }
    }

    private async Task DeleteAllLogs()
    {
        if (_connection == null)
        {
            return;
        }

        await SafeInvokeAsync(() => _connection.InvokeAsync("DeleteAllLogs"));

        await FetchLogFiles();
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
