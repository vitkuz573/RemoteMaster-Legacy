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
    private readonly List<string> _messages = [];

    private ClaimsPrincipal? _user;

    private bool _disposed;

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        _connection = new HubConnectionBuilder()
            .WithUrl($"http://{Host}:5555/hubs/chat")
            .Build();

        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            var encodedMsg = $"{user}: {message}";

            _messages.Add(encodedMsg);

            InvokeAsync(StateHasChanged);
        });

        await _connection.StartAsync();
    }

    private async Task Send()
    {
        await _connection.SendAsync("SendMessage", _user.FindFirstValue(ClaimTypes.Name), _message);

        _message = string.Empty;
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