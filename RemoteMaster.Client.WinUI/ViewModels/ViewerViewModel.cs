using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace RemoteMaster.Client.WinUI.ViewModels;

public partial class ViewerViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string _imageUrl;

    private HubConnection _serverConnection;

    public string Host
    {
        get;
        set;
    }

    public ViewerViewModel()
    {
    }

    public void InitializeServerConnection()
    {
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