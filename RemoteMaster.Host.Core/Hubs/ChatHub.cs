﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize(Policy = "LocalhostOrAuthenticatedPolicy")]
public class ChatHub(IChatInstanceService chatInstanceService) : Hub<IChatClient>
{
    private static readonly ConcurrentQueue<ChatMessage> Messages = new();

    public async override Task OnConnectedAsync()
    {
        foreach (var message in Messages)
        {
            var chatMessageDto = new ChatMessageDto(message.User, message.Message)
            {
                Id = message.Id,
                Timestamp = message.Timestamp,
                ReplyToId = message.ReplyToId,
            };

            chatMessageDto.Attachments.AddRange(message.Attachments.Select(a => new AttachmentDto(a.FileName, a.Data, a.MimeType)));

            await Clients.Caller.ReceiveMessage(chatMessageDto);
        }

        await base.OnConnectedAsync();
    }

    [Authorize(Policy = "SendMessagePolicy")]
    [HubMethodName("SendMessage")]
    public async Task SendMessageAsync(ChatMessageDto chatMessageDto)
    {
        ArgumentNullException.ThrowIfNull(chatMessageDto);

        if (!await chatInstanceService.IsRunningAsync())
        {
            await chatInstanceService.StartAsync();
        }

        var id = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.Now;

        var attachments = chatMessageDto.Attachments.Select(a => new Attachment(a.FileName, a.Data, a.MimeType)).ToList();

        var chatMessage = new ChatMessage(id, chatMessageDto.User, chatMessageDto.Message, timestamp, attachments, chatMessageDto.ReplyToId);

        Messages.Enqueue(chatMessage);

        while (Messages.Count > 100)
        {
            Messages.TryDequeue(out _);
        }

        chatMessageDto.Id = id;
        chatMessageDto.Timestamp = timestamp;

        await Clients.All.ReceiveMessage(chatMessageDto);
    }

    [Authorize(Policy = "DeleteMessagePolicy")]
    [HubMethodName("DeleteMessage")]
    public async Task DeleteMessageAsync(string id, string user)
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

            await Clients.All.MessageDeleted(id);
        }
    }

    [HubMethodName("Typing")]
    public async Task TypingAsync(string user)
    {
        await Clients.Others.UserTyping(user);
    }

    [HubMethodName("StopTyping")]
    public async Task StopTypingAsync(string user)
    {
        await Clients.Others.UserStopTyping(user);
    }
}
