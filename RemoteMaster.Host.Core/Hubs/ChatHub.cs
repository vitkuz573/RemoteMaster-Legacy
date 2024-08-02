// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;

namespace RemoteMaster.Host.Core.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentQueue<(string Id, string User, string Message)> Messages = new();

    public async Task SendMessage(string user, string message)
    {
        const string processName = "RemoteMaster.Host.Chat";
        const string processPath = @"C:\Program Files\RemoteMaster\Host\RemoteMaster.Host.Chat.exe";
        const string workingDirectory = @"C:\Program Files\RemoteMaster\Host";

        var isProcessRunning = Process.GetProcessesByName(processName).Length != 0;

        if (!isProcessRunning)
        {
            if (File.Exists(processPath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            else
            {
                throw new FileNotFoundException($"The specified file '{processPath}' was not found.");
            }
        }

        var id = Guid.NewGuid().ToString();

        Messages.Enqueue((id, user, message));

        while (Messages.Count > 100)
        {
            Messages.TryDequeue(out _);
        }

        await Clients.All.SendAsync("ReceiveMessage", id, user, message);
    }

    public async Task DeleteMessage(string id, string user)
    {
        var messagesList = Messages.ToList();
        var messageToRemove = messagesList.FirstOrDefault(m => m.Id == id && m.User == user);

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