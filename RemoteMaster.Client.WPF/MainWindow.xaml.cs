using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RemoteMaster.Client.WPF;

public partial class MainWindow : Window
{
    private readonly HubConnection? _agentConnection;
    private readonly HubConnection? _serverConnection;

    public MainWindow()
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

        _serverConnection.On<ChunkWrapper>("ScreenUpdate", async chunk =>
        {
            if (Chunker.TryUnchunkify(chunk, out var allData))
            {
                Dispatcher.Invoke(() =>
                {
                    var bitmapImage = new BitmapImage();
                    using (var memory = new MemoryStream(allData))
                    {
                        memory.Position = 0;
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memory;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                    }

                    screenImage.Source = bitmapImage;
                });
            }
        });

        _serverConnection.StartAsync();
    }

    private static bool IsConnectionReady(HubConnection connection) => connection != null && connection.State == HubConnectionState.Connected;
}
