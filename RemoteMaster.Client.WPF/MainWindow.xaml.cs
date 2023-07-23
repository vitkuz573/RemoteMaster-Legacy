using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

        _serverConnection.On<ScreenDataDto>("ScreenData", dto =>
        {
            var screenNumber = 1;

            foreach (var display in dto.Displays)
            {
                Dispatcher.Invoke(() =>
                {
                    var newItem = new MenuItem
                    {
                        Header = $"Screen {screenNumber++} ({display.Item3.Width} x {display.Item3.Height})",
                        Tag = display.Item1
                    };

                    if (display.Item2)
                    {
                        newItem.IsChecked = true;
                    }

                    displays.Items.Add(newItem);
                });
            }
        });

        _serverConnection.On<ChunkWrapper>("ScreenUpdate", chunk =>
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

    private async Task TryInvokeServerAsync(string method)
    {
        if (IsConnectionReady(_serverConnection))
        {
            await _serverConnection.InvokeAsync(method);
        }
    }

    private async Task TryInvokeServerAsync<T>(string method, T argument)
    {
        if (IsConnectionReady(_serverConnection))
        {
            await _serverConnection.InvokeAsync(method, argument);
        }
    }

    private (double, double) GetRelativeMousePositionOnPercent(MouseEventArgs e)
    {
        var currentPosition = e.GetPosition(screenImage);

        var percentX = currentPosition.X / screenImage.ActualWidth;
        var percentY = currentPosition.Y / screenImage.ActualHeight;

        return (percentX, percentY);
    }

    private async void OnMouseMove(object sender, MouseEventArgs e)
    {
        var xyPercent = GetRelativeMousePositionOnPercent(e);

        var dto = new MouseMoveDto
        {
            X = xyPercent.Item1,
            Y = xyPercent.Item2
        };

        await TryInvokeServerAsync("SendMouseCoordinates", dto);
    }

    private static bool IsConnectionReady(HubConnection connection) => connection != null && connection.State == HubConnectionState.Connected;

    private async void OnDisplayClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem)
        {
            await TryInvokeServerAsync("SendSelectedScreen", menuItem.Tag);
        }
    }

    private async void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        await TryInvokeServerAsync("SetQuality", (int)e.NewValue);
    }
}
