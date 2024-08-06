// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Models;
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
    private string _replyToMessage = string.Empty;
    private string? _replyToMessageId = null;
    private readonly List<ChatMessageDto> _messages = [];
    private string _typingMessage = string.Empty;
    private ClaimsPrincipal? _user;
    private bool _disposed;
    private Timer? _typingTimer;
    private List<IBrowserFile> _selectedFiles = [];

    protected async override Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/chat")
            .Build();

        _connection.On<ChatMessageDto>("ReceiveMessage", chatMessageDto =>
        {
            _messages.Add(chatMessageDto);

            InvokeAsync(StateHasChanged);
        });

        _connection.On<string>("MessageDeleted", id =>
        {
            var messageToRemove = _messages.FirstOrDefault(m => m.Id == id);

            if (messageToRemove != default)
            {
                _messages.Remove(messageToRemove);

                InvokeAsync(StateHasChanged);
            }
        });

        _connection.On<string>("UserTyping", user =>
        {
            _typingMessage = $"{user} is typing...";

            InvokeAsync(StateHasChanged);
        });

        _connection.On<string>("UserStopTyping", user =>
        {
            _typingMessage = string.Empty;

            InvokeAsync(StateHasChanged);
        });

        await _connection.StartAsync();
    }

    private async Task Send()
    {
        var attachments = new List<AttachmentDto>();

        foreach (var file in _selectedFiles)
        {
            var buffer = new byte[file.Size];
            using var stream = file.OpenReadStream(file.Size);
            await stream.ReadAsync(buffer);
            attachments.Add(new AttachmentDto
            {
                FileName = file.Name,
                Data = buffer,
                MimeType = file.ContentType
            });
        }

        var chatMessageDto = new ChatMessageDto
        {
            User = _user.FindFirstValue(ClaimTypes.Name),
            Message = _message,
            ReplyToId = _replyToMessage,
            Attachments = attachments
        };

        await _connection.SendAsync("SendMessage", chatMessageDto);

        _message = string.Empty;
        _replyToMessageId = null;
        _replyToMessage = string.Empty;
        _selectedFiles.Clear();
    }

    private async Task Delete(string id)
    {
        await _connection.SendAsync("DeleteMessage", id, _user.FindFirstValue(ClaimTypes.Name));
    }

    private void SetReplyToMessage(string messageId)
    {
        _replyToMessageId = messageId;
        _replyToMessage = GetReplyToMessage(messageId);
    }

    private void ClearReply()
    {
        _replyToMessageId = null;
        _replyToMessage = string.Empty;
    }

    private string GetReplyToMessage(string messageId)
    {
        var message = _messages.FirstOrDefault(m => m.Id == messageId);

        return message != null ? string.Concat(message.Message.AsSpan(0, Math.Min(30, message.Message.Length)), "...") : string.Empty;
    }

    private void HandleInput(ChangeEventArgs e)
    {
        _message = e.Value?.ToString() ?? string.Empty;
        _connection.SendAsync("Typing", _user.FindFirstValue(ClaimTypes.Name)).GetAwaiter().GetResult();
        ResetTypingTimer();
    }

    private void HandleFileChange(InputFileChangeEventArgs e)
    {
        _selectedFiles.AddRange(e.GetMultipleFiles());
    }

    private void RemoveFile(IBrowserFile file)
    {
        _selectedFiles.Remove(file);
    }

    private void ResetTypingTimer()
    {
        _typingTimer?.Dispose();
        _typingTimer = new Timer(async _ =>
        {
            await _connection.SendAsync("StopTyping", _user.FindFirstValue(ClaimTypes.Name));
        }, null, 1000, Timeout.Infinite);
    }

    private async void OpenFileDialog()
    {
        await JsRuntime.InvokeVoidAsync("document.getElementById('fileInput').click");
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