// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Chat.Avalonia.Commands;
using RemoteMaster.Host.Chat.Avalonia.Models;

namespace RemoteMaster.Host.Chat.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private HubConnection _connection;
    private string _newMessage;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public string CurrentUser { get; } = "User";

    public string NewMessage
    {
        get => _newMessage;
        set
        {
            if (RaiseAndSetIfChanged(ref _newMessage, value))
            {
                ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand SendMessageCommand { get; }

    public ICommand DeleteMessageCommand { get; }

    public MainWindowViewModel()
    {
        SendMessageCommand = new RelayCommand(
            async () => await SendMessageAsync(),
            () => !string.IsNullOrWhiteSpace(NewMessage)
        );
        DeleteMessageCommand = new RelayCommand<string>(
            async id => await DeleteMessageAsync(id),
            id => !string.IsNullOrEmpty(id)
        );

        InitializeConnection();
    }

    private void InitializeConnection()
    {
#pragma warning disable CA2000
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
#pragma warning restore CA2000

        _connection = new HubConnectionBuilder()
            .WithUrl("https://127.0.0.1:5001/hubs/chat", options =>
            {
                options.HttpMessageHandlerFactory = _ => httpClientHandler;
            })
            .AddMessagePackProtocol()
            .Build();

        _connection.On<ChatMessage>("ReceiveMessage", chatMessage =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Messages.Add(chatMessage);
            });
        });

        _connection.On<string>("MessageDeleted", id =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var messageToRemove = Messages.FirstOrDefault(m => m.Id == id);

                if (messageToRemove != null)
                {
                    Messages.Remove(messageToRemove);
                }
            });
        });

        _connection.StartAsync();
    }

    public async Task SendMessageAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewMessage) && _connection.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("SendMessage", CurrentUser, NewMessage);

            NewMessage = string.Empty;
        }
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        if (!string.IsNullOrEmpty(messageId) && _connection.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("DeleteMessage", messageId, CurrentUser);
        }
    }
}

