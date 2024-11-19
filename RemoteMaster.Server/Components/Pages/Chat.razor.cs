// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Claims;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Polly;
using RemoteMaster.Shared.DTOs;
using RemoteMaster.Shared.Formatters;

namespace RemoteMaster.Server.Components.Pages;

[Authorize]
public partial class Chat : IAsyncDisposable
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    [Parameter]
    public string Host { get; set; } = default!;

    [Inject(Key = "Resilience-Pipeline")]
    public ResiliencePipeline<string> ResiliencePipeline { get; set; } = default!;

    private InputFile? _fileInput;
    private HubConnection? _connection;
    private string _message = string.Empty;
    private string _replyToMessage = string.Empty;
    private string? _replyToMessageId = null;
    private readonly List<ChatMessageDto> _messages = [];
    private string _typingMessage = string.Empty;
    private ClaimsPrincipal? _user;
    private bool _firstRenderCompleted;
    private bool _disposed;
    private Timer? _typingTimer;
    private readonly List<IBrowserFile> _selectedFiles = [];
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
        var authState = await AuthenticationStateTask;

        _user = authState.User;

        _connection = new HubConnectionBuilder()
            .WithUrl($"https://{Host}:5001/hubs/chat")
            .AddMessagePackProtocol(options =>
            {
                var resolver = CompositeResolver.Create([new IPAddressFormatter(), new PhysicalAddressFormatter()], [ContractlessStandardResolver.Instance]);

                options.SerializerOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            })
            .Build();

        _connection.On<ChatMessageDto>("ReceiveMessage", chatMessageDto =>
        {
            _messages.Add(chatMessageDto);

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

        _connection.On<string>("UserTyping", user =>
        {
            _typingMessage = $"{user} is typing...";

            InvokeAsync(StateHasChanged);
        });

        _connection.On<string>("UserStopTyping", _ =>
        {
            _typingMessage = string.Empty;

            InvokeAsync(StateHasChanged);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start connection.");
        }
    }

    private async Task Send()
    {
        if (_connection == null)
        {
            return;
        }

        var attachments = new List<AttachmentDto>();

        foreach (var file in _selectedFiles)
        {
            var buffer = new byte[file.Size];
            await using var stream = file.OpenReadStream(file.Size);
            var totalBytesRead = 0;
            int bytesRead;

            while (totalBytesRead < file.Size && (bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead))) > 0)
            {
                totalBytesRead += bytesRead;
            }

            attachments.Add(new AttachmentDto(file.Name, buffer, file.ContentType));
        }

        var user = _user?.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("User name is not found.");

        var chatMessageDto = new ChatMessageDto(user, _message)
        {
            ReplyToId = _replyToMessage,
        };

        chatMessageDto.Attachments.AddRange(attachments);

        await SafeInvokeAsync(() => _connection.SendAsync("SendMessage", chatMessageDto));

        _message = string.Empty;
        _replyToMessageId = null;
        _replyToMessage = string.Empty;
        _selectedFiles.Clear();
    }

    private async Task Delete(string id)
    {
        if (_connection == null)
        {
            return;
        }

        var userName = _user?.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("User name is not found.");

        await SafeInvokeAsync(() => _connection.SendAsync("DeleteMessage", id, userName));
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

    private async void HandleInput(ChangeEventArgs e)
    {
        if (_connection == null)
        {
            return;
        }

        var userName = _user?.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("User name is not found.");
        _message = e.Value?.ToString() ?? string.Empty;
        
        await SafeInvokeAsync(() => _connection.SendAsync("Typing", userName));

        ResetTypingTimer();
    }

    private async Task TriggerFileUpload()
    {
        var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/commonUtils.js");

        await module.InvokeVoidAsync("triggerClick", _fileInput!.Element);
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
        if (_connection == null)
        {
            return;
        }

        _typingTimer?.Dispose();
        _typingTimer = new Timer(_ =>
        {
            SendStopTyping();
            
            return;

            async void SendStopTyping()
            {
                var userName = _user?.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("User name is not found.");

                await SafeInvokeAsync(() => _connection.SendAsync("StopTyping", userName));
            }
        }, null, 1000, Timeout.Infinite);
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
