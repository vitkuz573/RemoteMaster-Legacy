using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;

namespace RemoteMaster.Client.Maui;

public partial class MainPage : ContentPage
{
    private readonly HubConnection _agentConnection;
    private readonly HubConnection _serverConnection;

    public MainPage()
    {
        InitializeComponent();

        _serverConnection = new HubConnectionBuilder()
            .WithUrl($"http://127.0.0.1:5076/hubs/control", options =>
            {
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
            })
            .AddMessagePackProtocol()
            .Build();

        _serverConnection.On<ChunkWrapper>("ScreenUpdate", chunk =>
        {
            if (Chunker.TryUnchunkify(chunk, out var allData))
            {
                screenImage.Dispatcher.DispatchAsync(() =>
                {
                    screenImage.Source = ImageSource.FromStream(() => new MemoryStream(allData));
                });
            }
        });

        _serverConnection.StartAsync();
    }

    private static bool IsConnectionReady(HubConnection connection) => connection != null && connection.State == HubConnectionState.Connected;


}