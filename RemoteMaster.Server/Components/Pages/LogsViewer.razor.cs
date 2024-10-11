// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class LogsViewer : IAsyncDisposable
{
    [Parameter]
    public string Host { get; set; } = default!;

    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    private HubConnection? _connection;
    private ClaimsPrincipal? _user;
    private List<string> _logFiles = [];
    private string? _selectedLogFile;
    private string _logContent = string.Empty;
    private string _selectedLogLevel = string.Empty;
    private DateTime? _startDate;
    private DateTime? _endDate;

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        _user = authState.User;

        await InitializeHostConnectionAsync();
        await FetchLogFiles();
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
            .AddMessagePackProtocol()
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

        _connection.On<string>("ReceiveMessage", message =>
        {
            _logContent = message;

            InvokeAsync(StateHasChanged);
        });

        _connection.On<string>("ReceiveError", errorMessage =>
        {
            _logContent = errorMessage;

            InvokeAsync(StateHasChanged);
        });

        await _connection.StartAsync();
    }

    private async Task FetchLogFiles()
    {
        if (_connection != null)
        {
            await _connection.InvokeAsync("GetLogFiles");
        }
    }

    private async Task OnLogSelected(ChangeEventArgs e)
    {
        _selectedLogFile = e.Value?.ToString();
        _logContent = string.Empty;

        await FetchLogs();
    }

    private async Task FetchLogs()
    {
        if (!string.IsNullOrEmpty(_selectedLogFile) && _connection != null)
        {
            await _connection.InvokeAsync("GetLog", _selectedLogFile);
        }
    }

    private async Task FetchFilteredLogs()
    {
        if (!string.IsNullOrEmpty(_selectedLogFile) && _connection != null)
        {
            await _connection.InvokeAsync("GetFilteredLog", _selectedLogFile, _selectedLogLevel, _startDate, _endDate);
        }
    }

    private async Task DeleteAllLogs()
    {
        if (_connection != null)
        {
            await _connection.InvokeAsync("DeleteAllLogs");
        }

        await FetchLogFiles();
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
                Logger.LogError($"An error occurred while asynchronously disposing the connection for host {Host}: {ex.Message}");
            }
        }

        GC.SuppressFinalize(this);
    }
}
