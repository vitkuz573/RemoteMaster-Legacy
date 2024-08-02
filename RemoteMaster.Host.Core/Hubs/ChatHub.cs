// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Host.Core.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentQueue<(string User, string Message)> Messages = new();

    public async Task SendMessage(string user, string message)
    {
        Messages.Enqueue((user, message));

        while (Messages.Count > 100)
        {
            Messages.TryDequeue(out _);
        }

        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async override Task OnConnectedAsync()
    {
        foreach (var (user, message) in Messages)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", user, message);
        }

        await base.OnConnectedAsync();
    }
}