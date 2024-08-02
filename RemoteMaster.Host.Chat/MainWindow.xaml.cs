// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Http;
using System.Windows;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Host.Chat;

public partial class MainWindow
{
    private HubConnection _connection;

    public MainWindow()
    {
        InitializeComponent();

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
            .Build();

        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            Dispatcher.Invoke(() =>
            {
                ChatTextBox.AppendText($"{user}: {message}\n");
            });
        });

        ConnectToHub();
    }

    private void ConnectToHub()
    {
        _connection.StartAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Dispatcher.Invoke(() =>
                {
                    ChatTextBox.AppendText("Connected to the chat hub.\n");
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    ChatTextBox.AppendText($"Failed to connect: {task.Exception?.GetBaseException().Message}\n");
                });
            }
        });
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var message = MessageTextBox.Text;

        if (_connection.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("SendMessage", "User", message);
            MessageTextBox.Clear();
        }
        else
        {
            MessageBox.Show("Connection is not established. Please reconnect.");
        }
    }

    private void ReconnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_connection.State != HubConnectionState.Connected)
        {
            ConnectToHub();
        }
        else
        {
            MessageBox.Show("Already connected.");
        }
    }
}