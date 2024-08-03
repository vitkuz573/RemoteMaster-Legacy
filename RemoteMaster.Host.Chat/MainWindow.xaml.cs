// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Host.Chat.Models;

namespace RemoteMaster.Host.Chat;

public partial class MainWindow : Window
{
    private HubConnection _connection;

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public static string CurrentUser => "User";

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;

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
            Dispatcher.Invoke(() =>
            {
                Messages.Add(chatMessage);
            });
        });

        _connection.On<string>("MessageDeleted", id =>
        {
            Dispatcher.Invoke(() =>
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

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var message = MessageTextBox.Text;

        if (_connection.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("SendMessage", CurrentUser, message);

            MessageTextBox.Clear();
        }
        else
        {
            MessageBox.Show("Connection is not established. Please reconnect.");
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var id = button?.Tag as string;

        if (!string.IsNullOrEmpty(id) && _connection.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("DeleteMessage", id, CurrentUser);
        }
        else
        {
            MessageBox.Show("Connection is not established. Please reconnect.");
        }
    }
}