using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Helpers;
using System;
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

    public static HubConnection? ServerConnection { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        InitializeConnectionAsync();
    }

    private async Task InitializeConnectionAsync()
    {
        try
        {
            ServerConnection = new HubConnectionBuilder()
                .WithUrl($"http://127.0.0.1:5076/hubs/control", options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                })
                .AddMessagePackProtocol()
                .Build();

            RegisterServerHandlers();

            await ServerConnection.StartAsync();
        }
        catch (Exception)
        {
            // Handle exceptions as needed
        }
    }

    private void RegisterServerHandlers()
    {
        ServerConnection?.On<ScreenDataDto>("ScreenData", dto =>
        {
            var screenNumber = 1;

            foreach (var display in dto.Displays)
            {
                Dispatcher.Invoke(() =>
                {
                    var menuItem = new MenuItem
                    {
                        Header = $"Screen {screenNumber++} ({display.Item3.Width} x {display.Item3.Height})",
                        Tag = display.Item1
                    };

                    menuItem.Click += OnDisplayClick;

                    if (display.Item2)
                    {
                        menuItem.IsChecked = true;
                    }

                    displays.Items.Add(menuItem);
                });
            }
        });

        ServerConnection?.On<ChunkWrapper>("ScreenUpdate", chunk =>
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
    }

    public static async Task TryInvokeServerAsync(string method)
    {
        if (IsConnectionReady(ServerConnection))
        {
            try
            {
                await ServerConnection?.InvokeAsync(method);
            }
            catch (Exception)
            {
                // Handle exception as needed
            }
        }
    }

    public static async Task TryInvokeServerAsync<T>(string method, T argument)
    {
        if (IsConnectionReady(ServerConnection))
        {
            try
            {
                await ServerConnection?.InvokeAsync(method, argument);
            }
            catch (Exception)
            {
                // Handle exception as needed
            }
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
            await TryInvokeServerAsync("SendSelectedScreen", Convert.ToString(menuItem.Tag));
            e.Handled = true;
        }
    }

    private async void QualitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        await TryInvokeServerAsync("SetQuality", (int)e.NewValue);
    }

    private void OnNewMessageBoxClick(object sender, RoutedEventArgs e)
    {
        var messageBoxWindow = new MessageBoxWindow();
        messageBoxWindow.ShowDialog();
    }

    private async void OnKillServerClick(object sender, RoutedEventArgs e)
    {
        await TryInvokeServerAsync("KillServer");
    }
}
