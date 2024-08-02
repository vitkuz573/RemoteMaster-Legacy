// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Host.Core.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentQueue<(string Id, string User, string Message)> Messages = new();

    public async Task SendMessage(string user, string message)
    {
        var id = Guid.NewGuid().ToString();
        Messages.Enqueue((id, user, message));

        while (Messages.Count > 100)
        {
            Messages.TryDequeue(out _);
        }

        await Clients.All.SendAsync("ReceiveMessage", id, user, message);
    }

    public async Task DeleteMessage(string id)
    {
        var messagesList = Messages.ToList();
        var messageToRemove = messagesList.FirstOrDefault(m => m.Id == id);

        if (messageToRemove != default)
        {
            messagesList.Remove(messageToRemove);
            Messages.Clear();

            foreach (var message in messagesList)
            {
                Messages.Enqueue(message);
            }

            await Clients.All.SendAsync("MessageDeleted", id);
        }
    }

    public async override Task OnConnectedAsync()
    {
        foreach (var (id, user, message) in Messages)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", id, user, message);
        }

        await base.OnConnectedAsync();
    }
}