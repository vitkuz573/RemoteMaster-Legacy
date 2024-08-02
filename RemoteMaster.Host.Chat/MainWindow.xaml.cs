// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using Microsoft.AspNetCore.SignalR.Client;

namespace RemoteMaster.Host.Chat;

public partial class MainWindow : Window
{
    private HubConnection _connection;

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ICommand DeleteCommand { get; }

    private string _currentUser = "User"; // Замените на фактическое имя пользователя

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        DeleteCommand = new RelayCommand<string>(DeleteMessage);

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

        _connection.On<string, string, string>("ReceiveMessage", (id, user, message) =>
        {
            Dispatcher.Invoke(() =>
            {
                Messages.Add(new ChatMessage { Id = id, User = user, Message = message });
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
            await _connection.SendAsync("SendMessage", _currentUser, message);
            MessageTextBox.Clear();
        }
        else
        {
            MessageBox.Show("Connection is not established. Please reconnect.");
        }
    }

    private async void DeleteMessage(string id)
    {
        if (_connection.State == HubConnectionState.Connected)
        {
            await _connection.SendAsync("DeleteMessage", id, _currentUser);
        }
        else
        {
            MessageBox.Show("Connection is not established. Please reconnect.");
        }
    }
}

public class ChatMessage
{
    public string Id { get; set; }

    public string User { get; set; }

    public string Message { get; set; }
}

public class RelayCommand<T>(Action<T> execute, Func<T, bool> canExecute = null) : ICommand
{
    private readonly Action<T> _execute = execute ?? throw new ArgumentNullException(nameof(execute));

    public bool CanExecute(object parameter)
    {
        return canExecute == null || canExecute((T)parameter);
    }

    public void Execute(object parameter)
    {
        _execute((T)parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
