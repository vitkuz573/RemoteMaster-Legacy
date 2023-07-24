using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace RemoteMaster.Client.WinUI.ViewModels;

public partial class ViewerViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _imageUrl;

    private readonly HubConnection _serverConnection;

    public string Host
    {
        get;
        set;
    }

    public ViewerViewModel()
    {
        Debug.WriteLine(Host);

        _serverConnection = new HubConnectionBuilder()
            .WithUrl($"http://{Host}:5076/hubs/control")
            .AddMessagePackProtocol()
            .Build();

        StartServerConnection();
    }

    private async void StartServerConnection()
    {
        try
        {
            await _serverConnection.StartAsync();
        }
        catch (Exception ex)
        {
            // Handle exception
        }
    }
}
