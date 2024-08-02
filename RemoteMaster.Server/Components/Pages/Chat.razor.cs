// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class Chat : IAsyncDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Parameter]
    public string Host { get; set; } = default!;

    private HubConnection? _connection;
    private string _message = string.Empty;
    private readonly List<(string Id, string User, string Message)> _messages = new();

    private ClaimsPrincipal? _user;

    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/chat")
            .Build();

        _connection.On<string, string, string>("ReceiveMessage", (id, user, message) =>
        {
            _messages.Add((id, user, message));
            InvokeAsync(StateHasChanged);
        });

        _connection.On<string>("MessageDeleted", id =>
        {
            var messageToRemove = _messages.FirstOrDefault(m => m.Id == id);

            if (messageToRemove == default)
            {
                return;
            }

            _messages.Remove(messageToRemove);

            InvokeAsync(StateHasChanged);
        });

        await _connection.StartAsync();
    }

    private async Task Send()
    {
        await _connection.SendAsync("SendMessage", _user.FindFirstValue(ClaimTypes.Name), _message);

        _message = string.Empty;
    }

    private async Task Delete(string id)
    {
        await _connection.SendAsync("DeleteMessage", id);
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
                Log.Error(ex, "An error occurred while asynchronously disposing the connection for host {Host}", Host);
            }
        }

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
